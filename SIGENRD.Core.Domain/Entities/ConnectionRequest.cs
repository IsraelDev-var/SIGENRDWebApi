

using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Exceptions;
using SIGENRD.Core.Domain.Rules;
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
        public int? InstallerCompanyId { get; set; }

        // Distribuidora receptora
        public int DistributorId { get; set; }

        // Transformador asociado
        public int TransformerId { get; set; }

        // Fecha de solicitud
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Conexión estándar o medición neta
        public RequestType RequestType { get; set; } = RequestType.Connection;

        // Capacidad requerida en kVA (RN-03, RN-15)
        public decimal RequestedCapacityKva { get; set; }

        // Capacidad que esta solicitud tiene reservada hoy sobre el transformador (RN-03)
        public decimal ReservedKva { get; set; }

        // Estado del trámite (RN-12)
        public RequestStatus Status { get; set; } = RequestStatus.Received;

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

        // ---- Flujo de estados (RN-12, RN-23) y reserva de capacidad (RN-03) ----

        private void MoveTo(RequestStatus next)
        {
            if (!RequestWorkflow.CanTransition(Status, next))
                throw new InvalidStateTransitionException(Status, next);

            Status = next;
        }

        /// <summary>Recibida a En revisión: reserva la capacidad solicitada.</summary>
        public void StartReview(Transformer transformer, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            // UnderReview se alcanza desde Received (aquí) o desde InCorrection (Resubmit): cada uno exige su origen.
            if (Status != RequestStatus.Received)
                throw new InvalidStateTransitionException(Status, RequestStatus.UnderReview);

            // Primero la reserva: si no hay capacidad la solicitud conserva su estado.
            transformer.Reserve(RequestedCapacityKva, availableThresholdPct, conditionedThresholdPct);
            ReservedKva = RequestedCapacityKva;
            Status = RequestStatus.UnderReview;
        }

        /// <summary>En revisión a Observaciones. La reserva se mantiene mientras el solicitante corrige.</summary>
        public void Observe() => MoveTo(RequestStatus.Observed);

        /// <summary>Observaciones a En corrección (lo hace el solicitante).</summary>
        public void StartCorrection() => MoveTo(RequestStatus.InCorrection);

        /// <summary>En corrección a En revisión. La capacidad ya está reservada.</summary>
        public void Resubmit()
        {
            if (Status != RequestStatus.InCorrection)
                throw new InvalidStateTransitionException(Status, RequestStatus.UnderReview);

            Status = RequestStatus.UnderReview;
        }

        /// <summary>
        /// En revisión a Aprobada (RN-15): la capacidad disponible debe cubrir la solicitada.
        /// Si no alcanza lanza InsufficientCapacityException y no cambia nada.
        /// </summary>
        public void Approve(Transformer transformer, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            if (!RequestWorkflow.CanTransition(Status, RequestStatus.Approved))
                throw new InvalidStateTransitionException(Status, RequestStatus.Approved);

            if (transformer.AvailableCapacityKva < RequestedCapacityKva)
                throw new InsufficientCapacityException(RequestedCapacityKva, transformer.AvailableCapacityKva);

            transformer.ConsumeReservation(ReservedKva, availableThresholdPct, conditionedThresholdPct);
            ReservedKva = 0;
            Status = RequestStatus.Approved;
        }

        /// <summary>En revisión a Rechazada: libera la reserva.</summary>
        public void Reject(Transformer transformer, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            MoveTo(RequestStatus.Rejected);
            ReleaseReserved(transformer, availableThresholdPct, conditionedThresholdPct);
        }

        /// <summary>Recibida, Observaciones o En corrección a Cancelada: libera la reserva si la había.</summary>
        public void Cancel(Transformer transformer, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            MoveTo(RequestStatus.Cancelled);
            ReleaseReserved(transformer, availableThresholdPct, conditionedThresholdPct);
        }

        private void ReleaseReserved(Transformer transformer, decimal availableThresholdPct, decimal conditionedThresholdPct)
        {
            if (ReservedKva <= 0) return;

            transformer.ReleaseReservation(ReservedKva, availableThresholdPct, conditionedThresholdPct);
            ReservedKva = 0;
        }
    }
}
