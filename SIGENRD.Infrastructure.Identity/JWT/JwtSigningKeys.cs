using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace SIGENRD.Infrastructure.Identity.JWT
{
    /// <summary>
    /// Llaves del JWT (RS256), cargadas una sola vez: la privada firma y solo la pública valida.
    /// </summary>
    public sealed class JwtSigningKeys
    {
        public const int MinimumKeySizeInBits = 2048;

        public SigningCredentials SigningCredentials { get; }

        /// <summary>Solo la parte pública: la validación nunca necesita la privada.</summary>
        public RsaSecurityKey ValidationKey { get; }

        private JwtSigningKeys(RSA rsa)
        {
            var keyId = Base64UrlEncoder.Encode(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()));

            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa) { KeyId = keyId }, SecurityAlgorithms.RsaSha256);

            ValidationKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)) { KeyId = keyId };
        }

        /// <summary>Carga la llave privada en PEM o Base64 (PKCS#8 o PKCS#1). Falla al arrancar si no sirve.</summary>
        public static JwtSigningKeys FromPrivateKey(string privateKey)
        {
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new InvalidOperationException("JwtSettings:PrivateKey no está configurada.");

            var rsa = RSA.Create();
            try
            {
                if (privateKey.Contains("-----BEGIN", StringComparison.Ordinal))
                    rsa.ImportFromPem(privateKey);
                else
                    ImportDer(rsa, Convert.FromBase64String(privateKey.Trim()));
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
            {
                rsa.Dispose();
                throw new InvalidOperationException(
                    "JwtSettings:PrivateKey no es una llave privada RSA válida (PEM o Base64 de PKCS#8).", ex);
            }

            if (rsa.KeySize < MinimumKeySizeInBits)
            {
                var size = rsa.KeySize;
                rsa.Dispose();
                throw new InvalidOperationException(
                    $"La llave RSA del JWT tiene {size} bits; el mínimo es {MinimumKeySizeInBits}.");
            }

            return new JwtSigningKeys(rsa);
        }

        private static void ImportDer(RSA rsa, byte[] der)
        {
            try
            {
                rsa.ImportPkcs8PrivateKey(der, out _);
            }
            catch (CryptographicException)
            {
                rsa.ImportRSAPrivateKey(der, out _);
            }
        }
    }
}
