namespace SIGENRD.Core.Domain.Enums
{
    /// <summary>
    /// Estados de una solicitud (RN-12). Cancelled se añade por RN-23.
    /// </summary>
    public enum RequestStatus
    {
        Received = 1,
        UnderReview = 2,
        Observed = 3,
        InCorrection = 4,
        Approved = 5,
        Rejected = 6,
        Cancelled = 7,
        /// <summary>Borrador (D-4): solo lo ve el solicitante; no reserva capacidad ni entra en la bandeja.</summary>
        Draft = 8
    }
}
