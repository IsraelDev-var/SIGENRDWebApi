using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Settings;
using MailKit.Net.Smtp;



namespace SIGENRD.Infrastructure.Shared.Services
{
    /// <summary>
    /// Envío real por SMTP (MailKit). La aplicación no lo llama directamente: pasa por la bandeja de salida
    /// (<see cref="OutboxEmailService"/>), salvo con la base en memoria, que no la soporta.
    /// </summary>
    public class SmtpEmailSender : IEmailSender, IEmailService
    {
        public MailSettings _mailSettings { get; }

        public SmtpEmailSender(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        Task IEmailService.SendAsync(EmailRequest request) => SendAsync(request);

        public async Task SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
        {
            // EmailRequest.From viene vacío (no nulo) por defecto: se usa el remitente configurado.
            var address = string.IsNullOrWhiteSpace(request.From) ? _mailSettings.EmailFrom : request.From;
            var from = new MailboxAddress(_mailSettings.DisplayName, address);

            var email = new MimeMessage();
            email.Sender = from;
            email.From.Add(from);
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;
            email.Body = new BodyBuilder { HtmlBody = request.Body }.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                // Conectar
                await smtp.ConnectAsync(
                    _mailSettings.SmtpHost,
                    _mailSettings.SmtpPort,
                    _mailSettings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                    cancellationToken);

                // Un servidor de desarrollo local no pide credenciales.
                if (!string.IsNullOrWhiteSpace(_mailSettings.SmtpUser))
                    await smtp.AuthenticateAsync(_mailSettings.SmtpUser, _mailSettings.SmtpPass, cancellationToken);

                // Enviar
                await smtp.SendAsync(email, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // El despachador de la bandeja de salida registra el error y reintenta.
                throw new InvalidOperationException("Error enviando correo: " + ex.Message, ex);
            }
            finally
            {
                if (smtp.IsConnected)
                    await smtp.DisconnectAsync(true);
            }
        }
    }
}
