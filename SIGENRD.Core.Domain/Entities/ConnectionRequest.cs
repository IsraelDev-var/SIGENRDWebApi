

using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;
using NetTopologySuite.Geometries;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una solicitud de conexión eléctrica por parte de un cliente o empresa instaladora.
    /// </summary>
    public class ConnectionRequest : AuditableEntity
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

        // Transformador asociado
        public int TransformerId { get; set; }

        // Fecha de solicitud
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Estado del trámite (UnderReview, Approved, etc.)
        public RequestStatus Status { get; set; } = RequestStatus.UnderReview;

        // Tipo de uso (residencial, comercial, etc.)
        public UsageType UsageType { get; set; }

        // Tipo de tarifa (BT, MT, AT)
        public TariffType TariffType { get; set; }

        // Tipo de interconexión (Parallel, Isolated, etc.)
        public InterconnectionType InterconnectionType { get; set; }

        // Descripción del proyecto
        public string? ProjectDescription { get; set; }

        // Dirección física
        public string? ProjectAddress { get; set; }

        // Coordenadas geográficas
        public Point? Coordinates { get; set; }

        // Relaciones
        public Customer? Customer { get; set; }
        public InstallerCompany? InstallerCompany { get; set; }
        public Distributor? Distributor { get; set; }
        public Transformer? Transformer { get; set; }

        public ICollection<TechnicalDocument>? TechnicalDocuments { get; set; }
        public ICollection<StateHistory>? StateHistories { get; set; }
        public ICollection<ReviewObservation>? Observations { get; set; }

        // Relación 1:1 con medición neta
        public NetMeteringRequest? NetMeteringRequest { get; set; }
    }
}
