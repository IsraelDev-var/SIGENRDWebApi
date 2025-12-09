using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;


namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : AuditableEntity
    {
        private readonly AppContextSIGENRD _context;
        protected readonly DbSet<TEntity> _dbSet;
        public GenericRepository(AppContextSIGENRD context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }



        public async Task AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public Task<TEntity?> GetByIdAsync(int id)
        {
            return _dbSet.FindAsync(id).AsTask();
        }

        public Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }
    }
}
