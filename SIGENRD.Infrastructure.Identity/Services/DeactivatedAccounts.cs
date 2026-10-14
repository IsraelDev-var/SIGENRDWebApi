using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Infrastructure.Identity.Configurations;

namespace SIGENRD.Infrastructure.Identity.Services
{
    /// <summary>
    /// Rechaza los tokens de una cuenta desactivada (B-5) aunque no hayan vencido: el JWT dura 60 minutos y la baja
    /// debe surtir efecto ya. Se consulta la base una vez cada pocos segundos por usuario; al desactivar o reactivar,
    /// <see cref="Forget"/> descarta lo guardado y el cambio rige en la petición siguiente.
    /// </summary>
    public class DeactivatedAccounts(IServiceScopeFactory scopes, IMemoryCache cache)
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

        private static string Key(string userId) => "deactivated:" + userId;

        public async Task<bool> IsDeactivatedAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (cache.TryGetValue(Key(userId), out bool deactivated)) return deactivated;

            await using var scope = scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContextSIGENRD>();
            deactivated = await context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.DeactivatedAt != null)
                .FirstOrDefaultAsync(cancellationToken);

            cache.Set(Key(userId), deactivated, Ttl);
            return deactivated;
        }

        public void Forget(string userId) => cache.Remove(Key(userId));
    }
}
