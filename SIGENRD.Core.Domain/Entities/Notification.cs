using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Notificación interna dentro del portal del usuario (RF-32).
    /// </summary>
    public class Notification : AuditableEntity
    {
        public int Id { get; set; }

        // Usuario de Identity destinatario
        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType NotificationType { get; set; }

        public bool IsRead { get; set; }
    }
}
