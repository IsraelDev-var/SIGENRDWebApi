using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class SimulationRepository(AppContextSIGENRD context) : ISimulationRepository
    {
        public async Task AddAsync(NetMeteringSimulation simulation, CancellationToken cancellationToken = default)
        {
            context.NetMeteringSimulations.Add(simulation);
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PagedResult<NetMeteringSimulation>> ListByUserAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = context.NetMeteringSimulations.AsNoTracking().Where(s => s.UserId == userId && s.IsActive);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .Include(s => s.Transformer)
                .OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<NetMeteringSimulation>(items, page, pageSize, total);
        }

        public Task<NetMeteringSimulation?> GetForUserAsync(int id, string userId, CancellationToken cancellationToken = default)
        {
            return Detailed().FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId && s.IsActive, cancellationToken);
        }

        public async Task<IReadOnlyList<NetMeteringSimulation>> GetManyForUserAsync(IReadOnlyCollection<int> ids, string userId, CancellationToken cancellationToken = default)
        {
            return await Detailed()
                .Where(s => ids.Contains(s.Id) && s.UserId == userId && s.IsActive)
                .OrderBy(s => s.CreatedAt).ThenBy(s => s.Id)
                .ToListAsync(cancellationToken);
        }

        private IQueryable<NetMeteringSimulation> Detailed() => context.NetMeteringSimulations.AsNoTracking()
            .Include(s => s.Generators)
            .Include(s => s.Transformer)
            .Include(s => s.Distributor);
    }
}
