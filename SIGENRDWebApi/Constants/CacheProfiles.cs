namespace SIGENRDWebApi.Constants
{
    /// <summary>Perfiles de caché HTTP (Cache-Control) de los controladores.</summary>
    public static class CacheProfiles
    {
        /// <summary>Consultas públicas del mapa: misma vigencia que la caché del servidor (MapCache:TtlSeconds).</summary>
        public const string Map = "mapa";
    }
}
