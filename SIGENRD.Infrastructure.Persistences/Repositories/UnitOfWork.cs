

using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

using System.Collections.Concurrent;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class UnitOfWork(AppContextSIGENRD context) : IUnitOfWork
    {
        private readonly AppContextSIGENRD _context = context;
        private readonly ConcurrentDictionary<string, object> _repositories = new();

        public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            var type = typeof(TEntity).Name;

            if (_repositories.ContainsKey(type))
                return (IGenericRepository<TEntity>)_repositories[type];

            var repositoryInstance = Activator.CreateInstance(typeof(GenericRepository<>).MakeGenericType(typeof(TEntity)), _context);

            _repositories.TryAdd(type, repositoryInstance!);
            return (IGenericRepository<TEntity>)repositoryInstance!;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
        }

        
    }
}
