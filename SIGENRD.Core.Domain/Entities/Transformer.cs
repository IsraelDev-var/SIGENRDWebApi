using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;



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

        // Capacidad reservada por solicitudes En revisión (RN-03)
        public decimal ReservedCapacityKva { get; set; }
        public TransformerStatus Status { get; set; } = TransformerStatus.Available;

        // CAMBIO AQUÍ: Usar Point para compatibilidad nativa con PostGIS
        // ColumnType "geography" es mejor para coordenadas GPS (lat/lon) que "geometry"
        public Point Location { get; set; } = default!;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public Distributor? Distributor { get; set; }
        public ServiceZone? ServiceZone { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }

        // ---- Comportamiento (RN-01, RN-02, RN-03) ----

        /// <summary>Porcentaje de disponibilidad efectiva, descontando reservas.</summary>
        public decimal AvailabilityPct =>
            TransformerAvailability.Percentage(TotalCapacityKva, AvailableCapacityKva, ReservedCapacityKva);

        public bool AcceptsNewRequests => TransformerAvailability.AcceptsNewRequests(Status);

        /// <summary>Recalcula el estado con los umbrales de la distribuidora. Maintenance no se toca.</summary>
        public void RecalculateStatus(decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            if (Status == TransformerStatus.Maintenance) return;

            Status = TransformerAvailability.Classify(AvailabilityPct, availableThresholdPct, conditionedThresholdPct);
        }

        public void UpdateCapacity(decimal totalKva, decimal availableKva, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            TotalCapacityKva = totalKva;
            AvailableCapacityKva = availableKva;
            LastUpdated = DateTime.UtcNow;
            RecalculateStatus(availableThresholdPct, conditionedThresholdPct);
        }

        public void EnterMaintenance()
        {
            Status = TransformerStatus.Maintenance;
            LastUpdated = DateTime.UtcNow;
        }

        public void LeaveMaintenance(decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            if (Status != TransformerStatus.Maintenance) return;

            Status = TransformerStatus.Available; // se reclasifica enseguida
            LastUpdated = DateTime.UtcNow;
            RecalculateStatus(availableThresholdPct, conditionedThresholdPct);
        }
    }
}