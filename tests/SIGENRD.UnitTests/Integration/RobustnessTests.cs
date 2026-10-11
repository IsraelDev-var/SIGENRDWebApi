using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Shared;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Regresión de los hallazgos P2 de la auditoría del 2026-10-10 contra PostGIS real:
/// DB-01 auditoría atómica, DB-02 índices de búsqueda y SCA-01 bandeja de salida de correo.
/// </summary>
public class RobustnessTests(PostgisFixture db) : IClassFixture<PostgisFixture>
{
    // ---------------- DB-01: auditoría atómica ----------------

    /// <summary>Simula que el registro de auditoría falla al guardarse.</summary>
    private sealed class FailingAuditInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("La auditoría no se pudo guardar.");

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private AppContextSIGENRD ContextWith(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql(db.ConnectionString, npgsql => npgsql.UseNetTopologySuite())
            .AddInterceptors(interceptors)
            .Options,
        new FakeCurrentUser(PostgisFixture.UserId));

    [FactIfDatabase]
    public async Task Si_falla_la_auditoria_el_cambio_se_revierte()
    {
        var name = "Zona sin auditoría " + Guid.NewGuid().ToString("N")[..8];

        await using (var context = ContextWith(new FailingAuditInterceptor()))
        {
            context.ServiceZones.Add(new ServiceZone { ZoneName = name });
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }

        await using var check = db.NewContext();
        Assert.False(await check.ServiceZones.AnyAsync(z => z.ZoneName == name));
    }

    [FactIfDatabase]
    public async Task Un_cambio_guardado_siempre_tiene_su_auditoria()
    {
        var name = "Zona auditada " + Guid.NewGuid().ToString("N")[..8];

        int id;
        await using (var context = ContextWith())
        {
            var zone = new ServiceZone { ZoneName = name };
            context.ServiceZones.Add(zone);
            await context.SaveChangesAsync();
            id = zone.Id;
        }

        await using var check = db.NewContext();
        Assert.True(await check.AuditLogs.AnyAsync(a =>
            a.EntityName == nameof(ServiceZone) && a.EntityId == id.ToString() && a.Action == "Added"));
    }

    [FactIfDatabase]
    public async Task Respeta_la_transaccion_que_abrio_el_llamador()
    {
        var name = "Zona revertida " + Guid.NewGuid().ToString("N")[..8];

        int id;
        await using (var context = ContextWith())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            var zone = new ServiceZone { ZoneName = name };
            context.ServiceZones.Add(zone);
            await context.SaveChangesAsync();
            id = zone.Id;
            await transaction.RollbackAsync();
        }

        // Si el contexto hubiera abierto y confirmado su propia transacción, quedaría guardado.
        await using var check = db.NewContext();
        Assert.False(await check.ServiceZones.AnyAsync(z => z.ZoneName == name));
        Assert.False(await check.AuditLogs.AnyAsync(a => a.EntityName == nameof(ServiceZone) && a.EntityId == id.ToString()));
    }

    // ---------------- DB-02: índices ----------------

    [FactIfDatabase]
    public async Task Existen_los_indices_de_busqueda_de_solicitudes()
    {
        await using var connection = new NpgsqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'ConnectionRequests'", connection);
        await using var reader = await command.ExecuteReaderAsync();

        var indexes = new Dictionary<string, string>();
        while (await reader.ReadAsync())
            indexes[reader.GetString(0)] = reader.GetString(1);

        Assert.Contains("IX_ConnectionRequests_Status_RequestedAt", indexes.Keys);
        Assert.Contains("IX_ConnectionRequests_RequestedAt", indexes.Keys);
        Assert.Contains("gin_trgm_ops", indexes["IX_ConnectionRequests_RequestCode_trgm"]);
        Assert.Contains("gin_trgm_ops", indexes["IX_ConnectionRequests_ProjectAddress_trgm"]);
    }

    // ---------------- SCA-01: bandeja de salida de correo ----------------

    private sealed class FakeSender(Func<EmailRequest, Task>? behavior = null) : IEmailSender
    {
        public System.Collections.Concurrent.ConcurrentBag<EmailRequest> Sent { get; } = new();

        public async Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            if (behavior is not null) await behavior(request);
            Sent.Add(request);
        }
    }

    private ServiceProvider Services(IEmailSender sender, int maxAttempts = 6)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EmailOutbox:MaxAttempts"] = maxAttempts.ToString(),
            ["EmailOutbox:BatchSize"] = "50"
        }).Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<AppContextSIGENRD>(o => o.UseNpgsql(db.ConnectionString, n => n.UseNetTopologySuite()))
            .AddSharedInfrastructure(configuration);

        services.RemoveAll<IEmailSender>();
        services.AddSingleton(sender);
        return services.BuildServiceProvider();
    }

    private static async Task EnqueueAsync(ServiceProvider services, string to, string body = "<p>Hola</p>")
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IEmailService>()
            .SendAsync(new EmailRequest { To = to, Subject = "Prueba de bandeja", Body = body });
    }

    private async Task<OutboxEmail> RowFor(string to)
    {
        await using var check = db.NewContext();
        return await check.OutboxEmails.AsNoTracking().SingleAsync(e => e.To == to);
    }

    private static string NewAddress() => "bandeja-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";

    [FactIfDatabase]
    public async Task El_correo_se_guarda_y_el_despachador_lo_envia_y_borra_el_cuerpo()
    {
        var sender = new FakeSender();
        await using var services = Services(sender);
        var to = NewAddress();

        await EnqueueAsync(services, to, "<p>código secreto</p>");
        var pending = await RowFor(to);
        Assert.Null(pending.SentAt);
        Assert.Equal("<p>código secreto</p>", pending.Body);

        await services.GetRequiredService<EmailOutboxDispatcher>().ProcessDueAsync();

        var sent = await RowFor(to);
        Assert.NotNull(sent.SentAt);
        Assert.Null(sent.Body);
        Assert.Contains(sender.Sent, e => e.To == to && e.Body == "<p>código secreto</p>");
    }

    [FactIfDatabase]
    public async Task Si_el_SMTP_falla_se_reintenta_mas_tarde_y_al_agotar_los_intentos_queda_fallido()
    {
        var sender = new FakeSender(_ => throw new InvalidOperationException("SMTP caído"));
        await using var services = Services(sender, maxAttempts: 2);
        var dispatcher = services.GetRequiredService<EmailOutboxDispatcher>();
        var to = NewAddress();

        await EnqueueAsync(services, to);
        await dispatcher.ProcessDueAsync();

        var first = await RowFor(to);
        Assert.Equal(1, first.Attempts);
        Assert.Null(first.FailedAt);
        Assert.Contains("SMTP caído", first.LastError);
        Assert.InRange(first.NextAttemptAt, DateTime.UtcNow.AddSeconds(30), DateTime.UtcNow.AddMinutes(2));

        // Un reintento antes de tiempo no lo toma.
        await dispatcher.ProcessDueAsync();
        Assert.Equal(1, (await RowFor(to)).Attempts);

        await using (var context = db.NewContext())
            await context.OutboxEmails.Where(e => e.To == to)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));
        await dispatcher.ProcessDueAsync();

        var last = await RowFor(to);
        Assert.Equal(2, last.Attempts);
        Assert.NotNull(last.FailedAt);
        Assert.Null(last.SentAt);
    }

    [FactIfDatabase]
    public async Task Dos_despachadores_a_la_vez_no_envian_el_mismo_correo_dos_veces()
    {
        // Envío lento para que los dos lotes se solapen.
        var sender = new FakeSender(_ => Task.Delay(30));
        await using var services = Services(sender);
        var dispatcher = services.GetRequiredService<EmailOutboxDispatcher>();
        var addresses = Enumerable.Range(0, 10).Select(_ => NewAddress()).ToList();

        foreach (var to in addresses)
            await EnqueueAsync(services, to);

        await Task.WhenAll(dispatcher.ProcessDueAsync(), dispatcher.ProcessDueAsync(), dispatcher.ProcessDueAsync());

        foreach (var to in addresses)
            Assert.Single(sender.Sent, e => e.To == to);
    }
}
