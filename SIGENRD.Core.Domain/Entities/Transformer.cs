using NetTopologySuite.Geometries; // 👈 Importante
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;


namespace SIGENRD.Core.Domain.Entities
{
    public class Transformer : AuditableEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DistributorId { get; set; }
        public int ServiceZoneId { get; set; }

        public decimal TotalCapacityKva { get; set; }
        public decimal AvailableCapacityKva { get; set; }
        public TransformerStatus Status { get; set; } = TransformerStatus.Available;

        // CAMBIO AQUÍ: Usar Point para compatibilidad nativa con PostGIS
        // ColumnType "geography" es mejor para coordenadas GPS (lat/lon) que "geometry"
        public Point Location { get; set; } = default!;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public Distributor? Distributor { get; set; }
        public ServiceZone? ServiceZone { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }
}