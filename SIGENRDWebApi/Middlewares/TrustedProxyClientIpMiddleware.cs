using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace SIGENRDWebApi.Middlewares
{
    public class TrustedProxySettings
    {
        public const string SectionName = "TrustedProxy";

        /// <summary>
        /// Clave compartida con el servidor del frontend (Next). Vacía: no se acepta ninguna IP reenviada.
        /// Secreto: va en Key Vault o User Secrets, nunca en appsettings.
        /// </summary>
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// El frontend llama a la API desde su servidor (BFF), así que la API ve la IP de ese servidor para todos los
    /// usuarios y el límite por IP (/Auth, seguimiento) se aplicaría al sitio entero. El frontend reenvía la IP
    /// del usuario con una clave compartida; solo con la clave correcta se usa como IP de la petición.
    /// Va primero en el pipeline: el límite de peticiones, los registros y la auditoría ven la IP del usuario.
    /// </summary>
    public class TrustedProxyClientIpMiddleware(RequestDelegate next, IOptions<TrustedProxySettings> settings, ILogger<TrustedProxyClientIpMiddleware> logger)
    {
        public const string KeyHeader = "X-EnergyMap-Proxy-Key";
        public const string ClientIpHeader = "X-EnergyMap-Client-IP";

        private readonly byte[] _key = Encoding.UTF8.GetBytes(settings.Value.Key ?? string.Empty);

        public Task Invoke(HttpContext context)
        {
            var headers = context.Request.Headers;
            var presented = headers[KeyHeader].ToString();
            var clientIp = headers[ClientIpHeader].ToString();

            // Nunca llegan al resto de la API, se usen o no.
            headers.Remove(KeyHeader);
            headers.Remove(ClientIpHeader);

            if (_key.Length > 0 && presented.Length > 0)
            {
                if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), _key))
                    logger.LogWarning("Clave de proxy incorrecta desde {RemoteIp}: se ignora la IP reenviada.", context.Connection.RemoteIpAddress);
                else if (IPAddress.TryParse(clientIp, out var ip))
                    context.Connection.RemoteIpAddress = ip;
            }

            return next(context);
        }
    }
}
