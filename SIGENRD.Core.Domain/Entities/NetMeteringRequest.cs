

using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una solicitud de conexión bajo el esquema de medición neta (generación distribuida).
    /// </summary>
    public class NetMeteringRequest : AuditableEntity
    {
        public int Id { get; set; }

        // Clave foránea 1:1 con ConnectionRequest
        public int ConnectionRequestId { get; set; }

        // Tipo de sistema (solar, eólico, etc.)
        public GenerationSystemType GenerationSystemType { get; set; }

        // Capacidad instalada en kW
        public decimal InstalledCapacityKw { get; set; }

        // Estado de la solicitud
        public RequestStatus Status { get; set; } = RequestStatus.UnderReview;

        // Fecha de aprobación
        public DateTime? ApprovedAt { get; set; }

        // Comentarios técnicos
        public string? Comments { get; set; }

        // Certificado de aprobación
        public string? ApprovalCertificateUrl { get; set; }

        public ConnectionRequest? ConnectionRequest { get; set; }
        public ICollection<Generator>? Generators { get; set; }
    }
}
