using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Repositories
{
    /// <summary>Perfil de cliente: uno por usuario de Identity.</summary>
    public interface ICustomerRepository
    {
        Task<Customer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        Task<bool> NationalIdExistsAsync(string nationalIdOrRnc, CancellationToken cancellationToken = default);

        Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    }

    /// <summary>Empresas instaladoras y su validación por la distribuidora (RN-05).</summary>
    public interface IInstallerCompanyRepository
    {
        Task<InstallerCompany?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Rastreada: se puede modificar y guardar.</summary>
        Task<InstallerCompany?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> RncExistsAsync(string rnc, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<InstallerCompany>> ListAsync(InstallerValidationStatus? status, CancellationToken cancellationToken = default);

        Task AddAsync(InstallerCompany company, CancellationToken cancellationToken = default);
    }

    public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

    /// <summary>Notificaciones internas del portal (RF-32).</summary>
    public interface INotificationRepository
    {
        Task<PagedResult<Notification>> ListAsync(string userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Rastreada, y solo si pertenece al usuario.</summary>
        Task<Notification?> GetForUserAsync(int id, string userId, CancellationToken cancellationToken = default);

        /// <summary>Marca todas como leídas y devuelve cuántas cambiaron.</summary>
        Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
    }
}
