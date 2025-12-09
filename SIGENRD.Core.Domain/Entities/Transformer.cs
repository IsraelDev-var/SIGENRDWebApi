
using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.ValueObjects;


namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un transformador de la red eléctrica.
    /// </summary>
    public class Transformer : AuditableEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int DistributorId { get; set; }
        public int ServiceZoneId { get; set; }

        // Capacidades
        public decimal TotalCapacityKva { get; set; }
        public decimal AvailableCapacityKva { get; set; }

        // Estado técnico del transformador
        public TransformerStatus Status { get; set; } = TransformerStatus.Available;

        // Ubicación geográfica
        
        // CAMBIO AQUÍ: Usar Point para compatibilidad nativa con PostGIS
        // ColumnType "geography" es mejor para coordenadas GPS (lat/lon) que "geometry"
        public Point Location { get; set; } = default!;

        // Fecha de actualización
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Relaciones
        public Distributor? Distributor { get; set; }
        public ServiceZone? ServiceZone { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }
}
