namespace SIGENRD.Infrastructure.Identity.JWT
{
    public class JwtSettings
    {
        /// <summary>
        /// Llave privada RSA (mínimo 2048 bits) con la que se firman los tokens (RS256).
        /// Acepta PEM o Base64 de PKCS#8 en una sola línea, más cómodo como variable de entorno.
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int DurationInMinutes { get; set; } = 60;
    }
}
