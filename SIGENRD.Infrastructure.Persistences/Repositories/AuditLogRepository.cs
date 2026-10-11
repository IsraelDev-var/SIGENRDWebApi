using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class AuditLogRepository(AppContextSIGENRD context) : IAuditLogRepository
    {
        public async Task<PagedResult<AuditLog>> SearchAsync(AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = context.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.EntityName)) query = query.Where(a => a.EntityName == filter.EntityName);
            if (!string.IsNullOrWhiteSpace(filter.EntityId)) query = query.Where(a => a.EntityId == filter.EntityId);
            if (!string.IsNullOrWhiteSpace(filter.UserId)) query = query.Where(a => a.UserId == filter.UserId);
            if (!string.IsNullOrWhiteSpace(filter.Action)) query = query.Where(a => a.Action == filter.Action);
            if (filter.From.HasValue) query = query.Where(a => a.CreatedAt >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(a => a.CreatedAt <= filter.To.Value);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AuditLog>(items, page, pageSize, total);
        }
    }
}
