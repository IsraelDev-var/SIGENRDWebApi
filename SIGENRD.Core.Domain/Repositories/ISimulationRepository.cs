using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Core.Domain.Repositories
{
    /// <summary>Simulaciones de medición neta. Cada usuario solo accede a las suyas.</summary>
    public interface ISimulationRepository
    {
        Task AddAsync(NetMeteringSimulation simulation, CancellationToken cancellationToken = default);

        /// <summary>Historial del usuario, más recientes primero (RF-26).</summary>
        Task<PagedResult<NetMeteringSimulation>> ListByUserAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Con equipos y transformador; null si no existe o es de otro usuario.</summary>
        Task<NetMeteringSimulation?> GetForUserAsync(int id, string userId, CancellationToken cancellationToken = default);

        /// <summary>Las del usuario entre los ids pedidos (RF-22); las ajenas se omiten.</summary>
        Task<IReadOnlyList<NetMeteringSimulation>> GetManyForUserAsync(IReadOnlyCollection<int> ids, string userId, CancellationToken cancellationToken = default);
    }
}
