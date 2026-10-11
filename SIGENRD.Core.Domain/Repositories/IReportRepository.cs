using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Repositories
{
    /// <summary>Una solicitud con lo que necesitan los reportes (RF-27 a RF-30): estado, zona, instaladora y fechas clave.</summary>
    public record RequestReportRow(
        int Id,
        string RequestCode,
        RequestType RequestType,
        RequestStatus Status,
        DateTime RequestedAt,
        decimal RequestedCapacityKva,
        string DistributorName,
        string ZoneName,
        int? InstallerCompanyId,
        string? InstallerName,
        DateTime? FirstReviewAt,
        DateTime? ResolvedAt);

    public interface IReportRepository
    {
        /// <summary>Solicitudes presentadas en el período [from, to), con la primera revisión y la resolución (aprobada o rechazada).</summary>
        Task<IReadOnlyList<RequestReportRow>> GetRequestRowsAsync(DateTime from, DateTime to, int? distributorId, CancellationToken cancellationToken = default);
    }
}
