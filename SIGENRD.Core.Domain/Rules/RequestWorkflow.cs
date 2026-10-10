using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules
{
    /// <summary>
    /// Tabla de transiciones válidas de una solicitud (RN-12, RN-23). Cualquier otra se rechaza.
    /// Aprobada, Rechazada y Cancelada son finales.
    /// </summary>
    public static class RequestWorkflow
    {
        private static readonly IReadOnlyDictionary<RequestStatus, RequestStatus[]> Allowed =
            new Dictionary<RequestStatus, RequestStatus[]>
            {
                [RequestStatus.Received] = new[] { RequestStatus.UnderReview, RequestStatus.Cancelled },
                [RequestStatus.UnderReview] = new[] { RequestStatus.Observed, RequestStatus.Approved, RequestStatus.Rejected },
                [RequestStatus.Observed] = new[] { RequestStatus.InCorrection, RequestStatus.Cancelled },
                [RequestStatus.InCorrection] = new[] { RequestStatus.UnderReview, RequestStatus.Cancelled },
                [RequestStatus.Approved] = Array.Empty<RequestStatus>(),
                [RequestStatus.Rejected] = Array.Empty<RequestStatus>(),
                [RequestStatus.Cancelled] = Array.Empty<RequestStatus>()
            };

        public static bool CanTransition(RequestStatus from, RequestStatus to) =>
            Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

        public static IReadOnlyList<RequestStatus> NextStates(RequestStatus from) =>
            Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<RequestStatus>();

        public static bool IsFinal(RequestStatus status) => NextStates(status).Count == 0;
    }
}
