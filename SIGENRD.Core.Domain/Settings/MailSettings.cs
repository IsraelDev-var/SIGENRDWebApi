

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
    }
}
