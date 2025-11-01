

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una solicitud de conexión bajo el esquema de medición neta (generación distribuida).
    /// </summary>
    public class NetMeteringRequest
    {
        public int Id { get; set; } // Igual que ConnectionRequest.Id

        // Tipo de sistema (Solar, Eólico, etc.)
        public string GenerationSystemType { get; set; } = string.Empty;

        // Capacidad instalada en kW
        public decimal InstalledCapacityKw { get; set; }

        // Fecha de aprobación
        public DateTime? ApprovedAt { get; set; }

        // Comentarios u observaciones técnicas
        public string? Comments { get; set; }

        // URL del certificado de aprobación
        public string? ApprovalCertificateUrl { get; set; }

        // Relación 1:1 con la solicitud principal
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
