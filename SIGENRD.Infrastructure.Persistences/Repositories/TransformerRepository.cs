using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class TransformerRepository(AppContextSIGENRD context) : ITransformerRepository
    {
        // Las lecturas del mapa no se rastrean: son de solo lectura (CQRS).
        private IQueryable<Transformer> ReadOnly => context.Transformers.AsNoTracking();

        public async Task<IReadOnlyList<Transformer>> GetInBoundsAsync(
            double minLatitude, double minLongitude, double maxLatitude, double maxLongitude,
            TransformerFilter filter, int limit, CancellationToken cancellationToken = default)
        {
            return await ReadOnly
                .InBounds(minLatitude, minLongitude, maxLatitude, maxLongitude, filter, limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Transformer>> GetNearAsync(
            double latitude, double longitude, double radiusMeters,
            TransformerFilter filter, int limit, CancellationToken cancellationToken = default)
        {
            return await ReadOnly
                .Near(latitude, longitude, radiusMeters, filter, limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Transformer>> SearchForManagementAsync(
            TransformerManagementFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = ReadOnly;

            if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                var escaped = filter.Code.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                query = query.Where(t => EF.Functions.ILike(t.Code, "%" + escaped + "%"));
            }
            if (filter.ServiceZoneId.HasValue) query = query.Where(t => t.ServiceZoneId == filter.ServiceZoneId);
            if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status);

            var total = await query.CountAsync(cancellationToken);

            // La disponibilidad es (disponible − reservada) / total, la misma cuenta que AvailabilityPct, escrita para SQL.
            var ordered = filter.Sort switch
            {
                TransformerSort.Availability => query
                    .OrderBy(t => t.TotalCapacityKva > 0 ? (t.AvailableCapacityKva - t.ReservedCapacityKva) / t.TotalCapacityKva : 0)
                    .ThenBy(t => t.Code),
                TransformerSort.LastUpdated => query.OrderBy(t => t.LastUpdated).ThenBy(t => t.Code),
                _ => query.OrderBy(t => t.Code)
            };

            var items = await ordered
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Include(t => t.ServiceZone)
                .Include(t => t.Distributor)
                .ToListAsync(cancellationToken);

            return new PagedResult<Transformer>(items, page, pageSize, total);
        }

        public async Task<IReadOnlyList<Transformer>> SearchByCodeAsync(string term, int limit, CancellationToken cancellationToken = default)
        {
            // Los comodines de LIKE se escapan para que el término no cambie el significado de la búsqueda.
            var escaped = term
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = "%" + escaped + "%";

            return await ReadOnly
                .Where(t => EF.Functions.ILike(t.Code, pattern))
                .OrderBy(t => t.Code)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public Task<Transformer?> GetByIdAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default)
        {
            var query = context.Transformers
                .Include(t => t.Distributor)
                .Include(t => t.ServiceZone)
                .AsQueryable();

            if (!asTracking) query = query.AsNoTracking();

            return query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<IReadOnlySet<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var list = codes.Distinct().ToList();

            var found = await ReadOnly
                .Where(t => list.Contains(t.Code))
                .Select(t => t.Code)
                .ToListAsync(cancellationToken);

            return found.ToHashSet();
        }

        public async Task AddAsync(Transformer transformer, CancellationToken cancellationToken = default)
            => await context.Transformers.AddAsync(transformer, cancellationToken);

        public async Task AddRangeAsync(IEnumerable<Transformer> transformers, CancellationToken cancellationToken = default)
            => await context.Transformers.AddRangeAsync(transformers, cancellationToken);

        public async Task<IReadOnlyList<ZoneRiskSummary>> GetZoneRiskAsync(CancellationToken cancellationToken = default)
        {
            var rows = await ReadOnly
                .GroupBy(t => new { t.ServiceZoneId, t.ServiceZone!.ZoneName })
                .Select(g => new
                {
                    g.Key.ServiceZoneId,
                    g.Key.ZoneName,
                    Total = g.Count(),
                    Critical = g.Count(t => t.Status == TransformerStatus.Critical),
                    Saturated = g.Count(t => t.Status == TransformerStatus.Saturated)
                })
                .Where(r => r.Critical > 0 || r.Saturated > 0)
                .OrderByDescending(r => r.Saturated)
                .ThenByDescending(r => r.Critical)
                .ToListAsync(cancellationToken);

            return rows.Select(r => new ZoneRiskSummary(r.ServiceZoneId, r.ZoneName, r.Total, r.Critical, r.Saturated)).ToList();
        }
    }
}
