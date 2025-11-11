

using System.Drawing;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un transformador de la red eléctrica con capacidad total y disponible.
    /// </summary>
    public class Transformer
    {
        public int Id { get; set; }

        // Código único del transformador
        public string Code { get; set; } = string.Empty;

        // Distribuidora a la que pertenece
        public int DistributorId { get; set; }

        // Capacidad total en kva(kilovoltio ampere)
        public decimal TotalCapacityKVA { get; set; }

        // Capacidad disponible en kva(kilovoltio ampere)
        public decimal AvailableCapacityKVA { get; set; }

        // Estado actual (Disponible / Condicionada / Saturada)
        public string Status { get; set; } = "Available";

        // Coordenadas geográficas del transformador
        public Point Location { get; set; } = default!;

        // Zona de servicio asociada
        public int ServiceZoneId { get; set; }

        // Fecha de última actualización
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Relaciones
        public Distributor? Distributor { get; set; }
        public ServiceZone? ServiceZone { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }
}
