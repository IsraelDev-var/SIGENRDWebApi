namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Registro de auditoría. Solo admite inserción (RN-21): no hereda de AuditableEntity.
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }

        // Usuario de Identity que hizo la acción (null si fue el sistema)
        public string? UserId { get; set; }

        // Added, Modified o Deleted
        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        // JSON con los valores anterior y nuevo
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
