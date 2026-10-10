using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Core.Domain.Repositories
{
    public enum RequestScopeKind
    {
        /// <summary>Sin acceso a ninguna solicitud.</summary>
        None = 0,

        /// <summary>Todas: personal de la distribuidora y administración.</summary>
        All = 1,

        /// <summary>Las solicitudes de los clientes ligados al usuario (RN-22).</summary>
        Customer = 2,

        /// <summary>Las solicitudes de las instaladoras ligadas al usuario (RN-22).</summary>
        InstallerCompany = 3
    }

    /// <summary>Qué solicitudes puede ver el usuario actual.</summary>
    public record RequestScope(RequestScopeKind Kind, string? UserId = null);

    public interface IConnectionRequestRepository
    {
        /// <summary>Solicitudes visibles para el alcance indicado, más recientes primero.</summary>
        Task<IReadOnlyList<ConnectionRequest>> GetVisibleAsync(RequestScope scope, CancellationToken cancellationToken = default);

        /// <summary>Devuelve null si no existe o no es visible: no se revela que existe.</summary>
        Task<ConnectionRequest?> GetVisibleByIdAsync(int id, RequestScope scope, CancellationToken cancellationToken = default);
    }
}
