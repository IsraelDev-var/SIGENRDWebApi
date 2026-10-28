using Microsoft.Extensions.Caching.Memory;

namespace SIGENRD.Infrastructure.Identity.Services
{
    /// <summary>
    /// Permisos de subida ya usados, para que cada uno sirva una sola vez. Se recuerdan hasta que vencen (5 minutos).
    /// Vive en la memoria de la instancia: con varias instancias, un permiso robado podría usarse una vez más en otra
    /// durante esos minutos, y solo para el mismo usuario, la misma solicitud y el mismo tipo de documento.
    /// </summary>
    public class UsedUploadTickets(IMemoryCache cache)
    {
        private readonly object _gate = new();

        private static string Key(string ticketId) => "upload-ticket:" + ticketId;

        /// <summary>true la primera vez que se presenta el permiso; false si ya se había usado.</summary>
        public bool TryConsume(string ticketId, DateTimeOffset expiresAt)
        {
            lock (_gate)
            {
                if (cache.TryGetValue(Key(ticketId), out _)) return false;
                // Un minuto de margen por la tolerancia de reloj con la que se valida el vencimiento.
                cache.Set(Key(ticketId), true, expiresAt.AddMinutes(1));
                return true;
            }
        }
    }
}
