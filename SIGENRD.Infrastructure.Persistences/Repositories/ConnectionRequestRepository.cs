using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class ConnectionRequestRepository(AppContextSIGENRD context) : IConnectionRequestRepository
    {
        public async Task<PagedResult<ConnectionRequest>> SearchAsync(
            RequestScope scope, RequestSearchFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = Visible(scope);

            if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status.Value);
            if (filter.RequestType.HasValue) query = query.Where(r => r.RequestType == filter.RequestType.Value);
            if (filter.TransformerId.HasValue) query = query.Where(r => r.TransformerId == filter.TransformerId.Value);
            if (filter.From.HasValue) query = query.Where(r => r.RequestedAt >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(r => r.RequestedAt <= filter.To.Value);

            if (!string.IsNullOrWhiteSpace(filter.Text))
            {
                // Los comodines de LIKE se escapan para que el texto no cambie el significado de la búsqueda.
                var escaped = filter.Text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                var pattern = "%" + escaped + "%";
                query = query.Where(r => EF.Functions.ILike(r.RequestCode, pattern)
                                         || (r.ProjectAddress != null && EF.Functions.ILike(r.ProjectAddress, pattern)));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ConnectionRequest>(items, page, pageSize, total);
        }

        public Task<ConnectionRequest?> GetByCodeAsync(string requestCode, CancellationToken cancellationToken = default)
            => context.ConnectionRequests.AsNoTracking().FirstOrDefaultAsync(r => r.RequestCode == requestCode, cancellationToken);

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
                .Include(r => r.TechnicalDocuments!.Where(d => d.IsActive))
                .Include(r => r.NetMeteringRequest)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<TechnicalDocument>?> GetDocumentsAsync(int requestId, RequestScope scope, CancellationToken cancellationToken = default)
        {
            if (!await Visible(scope).AnyAsync(r => r.Id == requestId, cancellationToken))
                return null;

            return await context.TechnicalDocuments.AsNoTracking()
                .Where(d => d.ConnectionRequestId == requestId && d.IsActive)
                .OrderBy(d => d.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<TechnicalDocument?> GetDocumentAsync(int requestId, int documentId, RequestScope scope, CancellationToken cancellationToken = default)
        {
            var visible = Visible(scope);

            return context.TechnicalDocuments.AsNoTracking()
                .Where(d => d.Id == documentId && d.ConnectionRequestId == requestId && d.IsActive)
                .Where(d => visible.Any(r => r.Id == d.ConnectionRequestId))
                .FirstOrDefaultAsync(cancellationToken);
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
