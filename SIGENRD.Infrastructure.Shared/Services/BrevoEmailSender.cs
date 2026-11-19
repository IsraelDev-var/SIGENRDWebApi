using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Settings;

namespace SIGENRD.Infrastructure.Shared.Services
{
    /// <summary>
    /// Envío por la API HTTPS de Brevo (POST /v3/smtp/email). Sirve donde el hosting bloquea el SMTP saliente.
    /// Igual que <see cref="SmtpEmailSender"/>, la aplicación lo usa a través de la bandeja de salida.
    /// </summary>
    public class BrevoEmailSender : IEmailSender, IEmailService
    {
        public const string Endpoint = "https://api.brevo.com/v3/smtp/email";

        private readonly HttpClient _http;
        private readonly MailSettings _settings;

        public BrevoEmailSender(HttpClient http, IOptions<MailSettings> settings)
        {
            _http = http;
            _settings = settings.Value;
        }

        Task IEmailService.SendAsync(EmailRequest request) => SendAsync(request);

        public async Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.BrevoApiKey))
                throw new InvalidOperationException("Error enviando correo: falta MailSettings:BrevoApiKey.");

            var from = string.IsNullOrWhiteSpace(request.From) ? _settings.EmailFrom : request.From;
            var payload = new
            {
                sender = new { name = _settings.DisplayName, email = from },
                to = new[] { new { email = request.To } },
                subject = request.Subject,
                htmlContent = request.Body
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            message.Headers.Add("api-key", _settings.BrevoApiKey);
            message.Headers.Accept.ParseAdd("application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(message, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // El despachador de la bandeja de salida registra el error y reintenta.
                throw new InvalidOperationException("Error enviando correo: " + ex.Message, ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (detail.Length > 300) detail = detail[..300];
                    throw new InvalidOperationException(
                        "Error enviando correo: Brevo respondió " + (int)response.StatusCode + " " + detail);
                }
            }
        }
    }
}
