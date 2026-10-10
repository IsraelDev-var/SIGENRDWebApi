using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Exceptions;
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

        /// <summary>Capacidad libre para solicitudes nuevas: lo disponible menos lo ya reservado.</summary>
        public decimal EffectiveAvailableKva => AvailableCapacityKva - ReservedCapacityKva;

        /// <summary>RN-03: reserva capacidad para una solicitud que entra en revisión.</summary>
        public void Reserve(decimal kva, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            if (Status == TransformerStatus.Maintenance || EffectiveAvailableKva < kva)
                throw new InsufficientCapacityException(kva, Status == TransformerStatus.Maintenance ? 0 : EffectiveAvailableKva);

            ReservedCapacityKva += kva;
            LastUpdated = DateTime.UtcNow;
            RecalculateStatus(availableThresholdPct, conditionedThresholdPct);
        }

        /// <summary>RN-03: libera una reserva (solicitud rechazada o cancelada).</summary>
        public void ReleaseReservation(decimal kva, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            ReservedCapacityKva = Math.Max(0m, ReservedCapacityKva - kva);
            LastUpdated = DateTime.UtcNow;
            RecalculateStatus(availableThresholdPct, conditionedThresholdPct);
        }

        /// <summary>RN-03: al aprobar, la reserva pasa a descontarse de la capacidad disponible.</summary>
        public void ConsumeReservation(decimal kva, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            ReservedCapacityKva = Math.Max(0m, ReservedCapacityKva - kva);
            AvailableCapacityKva = Math.Max(0m, AvailableCapacityKva - kva);
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