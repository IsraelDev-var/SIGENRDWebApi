using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una empresa instaladora certificada por la SIE.
    /// </summary>
    public class InstallerCompany : AuditableEntity
    {
        public int Id { get; set; }

        // Nombre comercial de la empresa
        public string TradeName { get; set; } = string.Empty;

        // RNC de la empresa
        public string Rnc { get; set; } = string.Empty;

        // Correo de contacto
        public string? ContactEmail { get; set; }

        // Teléfono principal
        public string? ContactPhone { get; set; }

        // Dirección física
        public string? Address { get; set; }

        // Validación por la distribuidora (RN-05)
        public InstallerValidationStatus ValidationStatus { get; set; } = InstallerValidationStatus.Pending;

        // Usuario de Identity de la empresa
        public string? ApplicationUserId { get; set; }

        // Relaciones
        public ICollection<EngineerUser>? Engineers { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }

}
