using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SIGENRD.Infrastructure.Identity.Configurations;

namespace SIGENRD.Infrastructure.Identity.Services
{
    /// <summary>
    /// Estado de la cuenta que decide si un JWT todavía vale, aunque no haya vencido (dura 60 minutos):
    /// la cuenta desactivada (B-5) y el sello de seguridad (A28-08), que Identity cambia al cambiar o restablecer la
    /// contraseña y al desactivar la cuenta. Se consulta la base una vez cada pocos segundos por usuario; al cambiar algo,
    /// <see cref="Forget"/> descarta lo guardado y el cambio rige en la petición siguiente de esta instancia.
    /// </summary>
    public class AccountSessions(IServiceScopeFactory scopes, IMemoryCache cache)
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

        public const string FailedMessage = "La sesión ya no es válida: la contraseña cambió o la cuenta ya no existe.";
        public const string DeactivatedMessage = "La cuenta está desactivada.";

        private sealed record State(bool Exists, bool Deactivated, string? StampHash);

        private static string Key(string userId) => "account-session:" + userId;

        /// <summary>
        /// Huella del sello que va en el token. No se pone el sello tal cual: Identity lo usa como semilla de algunos
        /// códigos, y el token lo puede leer cualquiera que lo tenga.
        /// </summary>
        public static string StampHash(string? securityStamp) =>
            Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp ?? string.Empty)))[..22];

        public async Task<bool> IsDeactivatedAsync(string userId, CancellationToken cancellationToken = default)
            => (await GetAsync(userId, fresh: false, cancellationToken)).Deactivated;

        /// <summary>Null si la sesión vale; si no, el motivo para rechazar el token.</summary>
        public async Task<string?> ValidateAsync(string userId, string? stampHash, CancellationToken cancellationToken = default)
        {
            var state = await GetAsync(userId, fresh: false, cancellationToken);

            // Si no coincide, puede ser que esta instancia guarde el sello anterior a un cambio hecho en otra (o el token
            // recién emitido tras cambiar la contraseña): se vuelve a leer antes de rechazar.
            if (state.Exists && state.StampHash != stampHash)
                state = await GetAsync(userId, fresh: true, cancellationToken);

            if (!state.Exists) return FailedMessage;
            if (state.Deactivated) return DeactivatedMessage;
            return state.StampHash == stampHash ? null : FailedMessage;
        }

        private async Task<State> GetAsync(string userId, bool fresh, CancellationToken cancellationToken)
        {
            if (!fresh && cache.TryGetValue(Key(userId), out State? cached) && cached is not null) return cached;

            await using var scope = scopes.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContextSIGENRD>();
            var row = await context.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.DeactivatedAt, u.SecurityStamp })
                .FirstOrDefaultAsync(cancellationToken);

            var state = row is null
                ? new State(false, false, null)
                : new State(true, row.DeactivatedAt != null, StampHash(row.SecurityStamp));
            cache.Set(Key(userId), state, Ttl);
            return state;
        }

        public void Forget(string userId) => cache.Remove(Key(userId));
    }
}
