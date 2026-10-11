using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

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

    /// <summary>Filtros de la lista de solicitudes. Todos son opcionales.</summary>
    public record RequestSearchFilter(
        RequestStatus? Status = null,
        RequestType? RequestType = null,
        int? TransformerId = null,
        string? Text = null,
        DateTime? From = null,
        DateTime? To = null);

    public interface IConnectionRequestRepository
    {
        /// <summary>Búsqueda paginada dentro del alcance del usuario, más recientes primero.</summary>
        Task<PagedResult<ConnectionRequest>> SearchAsync(
            RequestScope scope, RequestSearchFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Seguimiento público por número de expediente. null si no existe.</summary>
        Task<ConnectionRequest?> GetByCodeAsync(string requestCode, CancellationToken cancellationToken = default);

        /// <summary>Devuelve null si no existe o no es visible: no se revela que existe.</summary>
        Task<ConnectionRequest?> GetVisibleByIdAsync(int id, RequestScope scope, CancellationToken cancellationToken = default);

        /// <summary>
        /// Carga la solicitud con lo que el flujo necesita (transformador y su distribuidora, documentos, solicitante)
        /// para modificarla. Respeta el alcance: null si no existe o no es del usuario.
        /// </summary>
        Task<ConnectionRequest?> GetForUpdateAsync(int id, RequestScope scope, CancellationToken cancellationToken = default);

        /// <summary>Historial de cambios de estado, del más antiguo al más reciente.</summary>
        Task<IReadOnlyList<StateHistory>> GetHistoryAsync(int requestId, CancellationToken cancellationToken = default);
    }
}
