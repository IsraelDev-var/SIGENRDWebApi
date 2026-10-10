using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Settings;
using MailKit.Net.Smtp;



namespace SIGENRD.Infrastructure.Shared.Services
{
    public class EmailService : IEmailService
    {
        public MailSettings _mailSettings { get; }

        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendAsync(EmailRequest request)
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
                    _mailSettings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                // Un servidor de desarrollo local no pide credenciales.
                if (!string.IsNullOrWhiteSpace(_mailSettings.SmtpUser))
                    await smtp.AuthenticateAsync(_mailSettings.SmtpUser, _mailSettings.SmtpPass);

                // Enviar
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                // Aquí podrías loguear el error con Serilog
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
