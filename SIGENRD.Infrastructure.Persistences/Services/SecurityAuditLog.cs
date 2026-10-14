using System.Text.Json;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Services
{
    /// <summary>
    /// Guarda los eventos de autenticación en AuditLogs, con el correo usado, la IP de origen y, si lo hay, el usuario
    /// que hizo el cambio (por ejemplo, el administrador que desactiva una cuenta).
    /// </summary>
    public class SecurityAuditLog(AppContextSIGENRD context, ICurrentUserService currentUser) : ISecurityAuditLog
    {
        public async Task RecordAsync(string action, string? userId, string email, CancellationToken cancellationToken = default)
        {
            context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = SecurityEvents.EntityName,
                EntityId = userId ?? string.Empty,
                NewValues = JsonSerializer.Serialize(new { email, ip = currentUser.IpAddress, by = currentUser.UserId }),
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
