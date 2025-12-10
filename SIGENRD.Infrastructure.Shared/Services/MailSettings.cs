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
            var email = new MimeMessage();

            // Remitente
            email.Sender = MailboxAddress.Parse(request.From ?? _mailSettings.EmailFrom);
            email.From.Add(MailboxAddress.Parse(request.From ?? _mailSettings.EmailFrom));

            // Destinatario
            email.To.Add(MailboxAddress.Parse(request.To));

            // Asunto
            email.Subject = request.Subject;

            // Cuerpo del correo
            var builder = new BodyBuilder();
            builder.HtmlBody = request.Body;
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                // Conectar
                await smtp.ConnectAsync(_mailSettings.SmtpHost, _mailSettings.SmtpPort, SecureSocketOptions.StartTls);

                // Autenticar
                await smtp.AuthenticateAsync(_mailSettings.SmtpUser, _mailSettings.SmtpPass);

                // Enviar
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                // Aquí podrías loguear el error con Serilog
                throw new Exception($"Error enviando correo: {ex.Message}");
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
