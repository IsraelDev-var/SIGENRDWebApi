using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.Diagnostics;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Shared.Services
{
    /// <summary>Entrega real de un correo (SMTP). Solo la usa el despachador de la bandeja de salida.</summary>
    public interface IEmailSender
    {
        Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
    }

    public class EmailOutboxSettings
    {
        public const string SectionName = "EmailOutbox";

        /// <summary>false: los correos se guardan pero no se envían (pruebas, mantenimiento del SMTP).</summary>
        public bool DispatcherEnabled { get; set; } = true;

        /// <summary>Revisión periódica de pendientes; un correo nuevo despierta al despachador al instante.</summary>
        public int PollIntervalSeconds { get; set; } = 30;

        public int BatchSize { get; set; } = 20;

        /// <summary>Intentos antes de darlo por fallido.</summary>
        public int MaxAttempts { get; set; } = 6;

        /// <summary>Días que se conservan los enviados antes de borrarlos.</summary>
        public int KeepSentDays { get; set; } = 7;
    }

    /// <summary>Aviso de "hay correo nuevo" para no esperar a la siguiente revisión periódica.</summary>
    public sealed class EmailOutboxSignal
    {
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

        public void Notify() => _channel.Writer.TryWrite(true);

        public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            try
            {
                await _channel.Reader.ReadAsync(cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Venció la espera: toca la revisión periódica.
            }
        }
    }

    /// <summary>
    /// IEmailService de la aplicación: guarda el correo en la bandeja de salida y vuelve enseguida (SCA-01).
    /// Usa un contexto propio para no guardar de paso cambios pendientes del llamador.
    /// </summary>
    public class OutboxEmailService(IServiceScopeFactory scopes, EmailOutboxSignal signal) : IEmailService
    {
        public async Task SendAsync(EmailRequest request)
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppContextSIGENRD>();

            var now = DateTime.UtcNow;
            db.OutboxEmails.Add(new OutboxEmail
            {
                To = request.To,
                Subject = request.Subject,
                Body = request.Body,
                From = string.IsNullOrWhiteSpace(request.From) ? null : request.From,
                CreatedAt = now,
                NextAttemptAt = now
            });
            await db.SaveChangesAsync();

            signal.Notify();
        }
    }

    /// <summary>
    /// Envía los correos pendientes con reintentos espaciados (1 min, 5 min, 15 min, 1 h, 4 h).
    /// Bloquea las filas con FOR UPDATE SKIP LOCKED: con varias instancias de la API, cada correo sale una vez.
    /// </summary>
    public class EmailOutboxDispatcher(
        IServiceScopeFactory scopes,
        EmailOutboxSignal signal,
        IOptionsMonitor<EmailOutboxSettings> settings,
        ILogger<EmailOutboxDispatcher> logger) : BackgroundService
    {
        private static readonly TimeSpan[] Backoff =
        {
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15),
            TimeSpan.FromHours(1), TimeSpan.FromHours(4)
        };

        public static TimeSpan DelayAfter(int attempts) => Backoff[Math.Clamp(attempts - 1, 0, Backoff.Length - 1)];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!settings.CurrentValue.DispatcherEnabled)
            {
                logger.LogInformation("Despachador de correo desactivado (EmailOutbox:DispatcherEnabled=false).");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Mientras haya lotes completos se sigue sin esperar.
                    while (await ProcessDueAsync(stoppingToken) >= settings.CurrentValue.BatchSize) { }
                    await PurgeSentAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Base caída u otro error: se registra y se vuelve a intentar en la próxima vuelta.
                    logger.LogError(ex, "Error procesando la bandeja de salida de correo");
                }

                await signal.WaitAsync(TimeSpan.FromSeconds(settings.CurrentValue.PollIntervalSeconds), stoppingToken);
            }
        }

        /// <summary>Procesa un lote de correos vencidos. Devuelve cuántos tomó.</summary>
        public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
        {
            var options = settings.CurrentValue;

            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppContextSIGENRD>();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var batch = await db.OutboxEmails
                .FromSql($"""
                    SELECT * FROM "OutboxEmails"
                    WHERE "SentAt" IS NULL AND "FailedAt" IS NULL AND "NextAttemptAt" <= {now}
                    ORDER BY "NextAttemptAt"
                    LIMIT {options.BatchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(cancellationToken);

            foreach (var email in batch)
            {
                try
                {
                    await sender.SendAsync(new EmailRequest
                    {
                        To = email.To,
                        Subject = email.Subject,
                        Body = email.Body ?? string.Empty,
                        From = email.From ?? string.Empty
                    }, cancellationToken);

                    email.SentAt = DateTime.UtcNow;
                    email.Body = null;
                    email.LastError = null;
                    EnergyMapMetrics.EmailSent();
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    email.Attempts++;
                    email.LastError = Truncate(ex.Message, 1000);

                    if (email.Attempts >= options.MaxAttempts)
                    {
                        email.FailedAt = DateTime.UtcNow;
                        EnergyMapMetrics.EmailFailed();
                        logger.LogError(ex, "Correo {EmailId} descartado tras {Attempts} intentos", email.Id, email.Attempts);
                    }
                    else
                    {
                        email.NextAttemptAt = DateTime.UtcNow + DelayAfter(email.Attempts);
                        EnergyMapMetrics.EmailRetry();
                        logger.LogWarning(ex, "Correo {EmailId} no enviado (intento {Attempts}); se reintenta a las {NextAttemptAt:o}",
                            email.Id, email.Attempts, email.NextAttemptAt);
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return batch.Count;
        }

        private async Task PurgeSentAsync(CancellationToken cancellationToken)
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppContextSIGENRD>();

            var limit = DateTime.UtcNow.AddDays(-settings.CurrentValue.KeepSentDays);
            await db.OutboxEmails.Where(e => e.SentAt != null && e.SentAt < limit).ExecuteDeleteAsync(cancellationToken);
        }

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
    }
}
