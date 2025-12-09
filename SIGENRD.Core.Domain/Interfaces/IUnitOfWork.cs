

using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
       
        IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;

        // Entities Repositories


        Task<int> SaveChangesAsync();
    }
}
