namespace SIGENRDWebApi.Options
{
    /// <summary>Orígenes del frontend que pueden llamar a la API desde el navegador. Vacío: ninguno.</summary>
    public class CorsSettings
    {
        public const string SectionName = "Cors";
        public const string PolicyName = "Frontend";

        public string[] AllowedOrigins { get; set; } = [];
    }

    /// <summary>Peticiones permitidas por IP y por ventana de tiempo en los endpoints anónimos sensibles.</summary>
    public class RateLimitSettings
    {
        public const string SectionName = "RateLimiting";

        public int AuthPermitLimit { get; set; } = 10;
        public int PublicTrackingPermitLimit { get; set; } = 30;
        public int WindowSeconds { get; set; } = 60;
    }
}
