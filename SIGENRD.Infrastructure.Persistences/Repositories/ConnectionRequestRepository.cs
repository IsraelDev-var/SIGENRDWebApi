using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class ConnectionRequestRepository(AppContextSIGENRD context) : IConnectionRequestRepository
    {
        public async Task<IReadOnlyList<ConnectionRequest>> GetVisibleAsync(RequestScope scope, CancellationToken cancellationToken = default)
        {
            return await Visible(scope)
                .OrderByDescending(r => r.RequestedAt)
                .ThenByDescending(r => r.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<ConnectionRequest?> GetVisibleByIdAsync(int id, RequestScope scope, CancellationToken cancellationToken = default)
        {
            return Visible(scope).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public Task<ConnectionRequest?> GetForUpdateAsync(int id, RequestScope scope, CancellationToken cancellationToken = default)
        {
            return Visible(scope, tracking: true)
                .Include(r => r.Customer)
                .Include(r => r.InstallerCompany)
                .Include(r => r.Transformer!).ThenInclude(t => t.Distributor)
                .Include(r => r.TechnicalDocuments)
                .Include(r => r.NetMeteringRequest)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<StateHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken = default)
        {
            return await context.StateHistories.AsNoTracking()
                .Where(h => h.ConnectionRequestId == requestId)
                .OrderBy(h => h.Id)
                .ToListAsync(cancellationToken);
        }

        /// <summary>RN-22: cada solicitante solo ve lo suyo; el personal de la distribuidora ve todo.</summary>
        private IQueryable<ConnectionRequest> Visible(RequestScope scope, bool tracking = false)
        {
            var query = tracking ? context.ConnectionRequests.AsQueryable() : context.ConnectionRequests.AsNoTracking();

            return scope.Kind switch
            {
                RequestScopeKind.All => query,
                RequestScopeKind.Customer when scope.UserId is not null =>
                    query.Where(r => r.Customer!.ApplicationUserId == scope.UserId),
                RequestScopeKind.InstallerCompany when scope.UserId is not null =>
                    query.Where(r => r.InstallerCompany!.ApplicationUserId == scope.UserId),
                _ => query.Where(r => false)
            };
        }
    }
}
