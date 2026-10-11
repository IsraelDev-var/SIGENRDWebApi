using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

namespace SIGENRDWebApi.Configuration
{
    /// <summary>
    /// Secretos desde Azure Key Vault (RNF de secretos, decisión de análisis): se activa con KeyVault:Uri.
    /// La API se autentica con su identidad administrada; no hay credenciales en la configuración.
    /// Los nombres de los secretos usan "--" en lugar de ":" (por ejemplo JwtSettings--PrivateKey).
    /// </summary>
    public static class KeyVaultConfiguration
    {
        public const string UriKey = "KeyVault:Uri";

        /// <summary>Identidad administrada asignada por el usuario; vacía usa la del sistema.</summary>
        public const string ManagedIdentityClientIdKey = "KeyVault:ManagedIdentityClientId";

        /// <returns>true si se agregó Key Vault como fuente de configuración.</returns>
        public static bool AddKeyVaultIfConfigured(this ConfigurationManager configuration)
        {
            var uri = configuration[UriKey];
            if (string.IsNullOrWhiteSpace(uri))
                return false;

            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = configuration[ManagedIdentityClientIdKey]
            });

            // Se agrega al final: los secretos del almacén ganan sobre appsettings y variables de entorno.
            configuration.AddAzureKeyVault(new Uri(uri), credential, new KeyVaultSecretManager());
            return true;
        }
    }
}
