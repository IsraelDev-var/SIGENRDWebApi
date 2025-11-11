using System;

namespace SIGENRD.Core.Domain.Base
{
    /// <summary>
    /// Base class for auditing entity changes (created/updated/deleted).
    /// </summary>
    public abstract class AuditableEntity
    {
        // Fecha de creación
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Usuario que creó el registro (puede vincularse con Identity)
        public string? CreatedBy { get; set; }

        // Fecha de última modificación
        public DateTime? UpdatedAt { get; set; }

        // Usuario que realizó la modificación
        public string? UpdatedBy { get; set; }

        // Si el registro está activo (soft delete)
        public bool IsActive { get; set; } = true;
    }
}

