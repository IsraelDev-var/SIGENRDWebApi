using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class ReportRepository(AppContextSIGENRD context) : IReportRepository
    {
        private static readonly string UnderReview = RequestStatus.UnderReview.ToString();
        private static readonly string Approved = RequestStatus.Approved.ToString();
        private static readonly string Rejected = RequestStatus.Rejected.ToString();

        public async Task<IReadOnlyList<RequestReportRow>> GetRequestRowsAsync(
            DateTime from, DateTime to, int? distributorId, CancellationToken cancellationToken = default)
        {
            var requests = context.ConnectionRequests.AsNoTracking()
                .Where(r => r.RequestedAt >= from && r.RequestedAt < to);

            if (distributorId.HasValue)
                requests = requests.Where(r => r.DistributorId == distributorId.Value);

            // Una sola consulta: las fechas clave salen del historial de estados (RN-10) con subconsultas.
            return await requests
                .OrderBy(r => r.RequestedAt).ThenBy(r => r.Id)
                .Select(r => new RequestReportRow(
                    r.Id,
                    r.RequestCode,
                    r.RequestType,
                    r.Status,
                    r.RequestedAt,
                    r.RequestedCapacityKva,
                    r.Distributor!.Name,
                    r.Transformer!.ServiceZone!.ZoneName,
                    r.InstallerCompanyId,
                    r.InstallerCompany != null ? r.InstallerCompany.TradeName : null,
                    context.StateHistories
                        .Where(h => h.ConnectionRequestId == r.Id && h.NewState == UnderReview)
                        .Min(h => (DateTime?)h.CreatedAt),
                    context.StateHistories
                        .Where(h => h.ConnectionRequestId == r.Id && (h.NewState == Approved || h.NewState == Rejected))
                        .Min(h => (DateTime?)h.CreatedAt)))
                .ToListAsync(cancellationToken);
        }
    }
}
