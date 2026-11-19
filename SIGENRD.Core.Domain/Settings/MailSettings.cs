

namespace SIGENRD.Core.Domain.Settings
{
    public class MailSettings
    {
        public string EmailFrom { get; set; } = string.Empty;
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; } = string.Empty;
        public string SmtpPass { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        // true para servidores reales (STARTTLS). false para un servidor local de desarrollo sin cifrado.
        public bool UseStartTls { get; set; } = true;

        // "Smtp" (por defecto) o "Brevo": la API HTTPS de Brevo, para hostings que bloquean el SMTP saliente
        // (por ejemplo, Railway fuera del plan Pro).
        public string Provider { get; set; } = "Smtp";

        // Clave de la API de Brevo (secreto). Solo con Provider = Brevo. EmailFrom debe ser un remitente verificado en Brevo.
        public string BrevoApiKey { get; set; } = string.Empty;
    }
}
