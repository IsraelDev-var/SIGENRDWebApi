namespace SIGENRDWebApi.Constants
{
    /// <summary>Nombres de las políticas de límite de peticiones (ver <see cref="Options.RateLimitSettings"/>).</summary>
    public static class RateLimitPolicies
    {
        /// <summary>Registro, inicio de sesión y confirmación de correo: frena la fuerza bruta y el abuso del correo.</summary>
        public const string Auth = "auth";

        /// <summary>Seguimiento público por número de expediente: frena la enumeración de códigos.</summary>
        public const string PublicTracking = "public-tracking";
    }
}
