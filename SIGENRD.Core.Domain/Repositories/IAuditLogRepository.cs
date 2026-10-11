using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Core.Domain.Repositories
{
    public record AuditLogFilter(
        string? EntityName, string? EntityId, string? UserId, string? Action, DateTime? From, DateTime? To);

    /// <summary>Consulta del registro de auditoría (RN-20). Solo lectura: la tabla no admite cambios (RN-21).</summary>
    public interface IAuditLogRepository
    {
        Task<PagedResult<AuditLog>> SearchAsync(AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
    }
}
