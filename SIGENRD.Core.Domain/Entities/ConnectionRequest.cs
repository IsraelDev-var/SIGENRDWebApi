

using System.Drawing;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una solicitud de conexión eléctrica por parte de un cliente o empresa instaladora.
    /// </summary>
    public class ConnectionRequest
    {
        public int Id { get; set; }

        // Código único de la solicitud
        public string RequestCode { get; set; } = string.Empty;

        // Cliente solicitante
        public int CustomerId { get; set; }

        // Empresa instaladora responsable
        public int InstallerCompanyId { get; set; }

        // Distribuidora receptora
        public int DistributorId { get; set; }

        // Transformador al que se conecta
        public int TransformerId { get; set; }

        // Fecha de solicitud
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow; 

        // Estado de la solicitud (En revisión, Aprobada, Rechazada, etc.)
        public string Status { get; set; } = "UnderReview";

        // Descripción del proyecto
        public string? ProjectDescription { get; set; }

        // Dirección o ubicación del proyecto
        public string? ProjectAddress { get; set; }

        // Coordenadas del proyecto
        public Point? Coordinates { get; set; }

        // Relaciones
        public Customer? Customer { get; set; }
        public InstallerCompany? InstallerCompany { get; set; }
        public Distributor? Distributor { get; set; }
        public Transformer? Transformer { get; set; }

        public ICollection<TechnicalDocument>? TechnicalDocuments { get; set; }
        public ICollection<StateHistory>? StateHistories { get; set; }
        public ICollection<ReviewObservation>? Observations { get; set; }
        public NetMeteringRequest? NetMeteringRequest { get; set; }
    }
}
