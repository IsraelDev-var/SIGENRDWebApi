namespace SIGENRDWebApi.Middlewares
{
    /// <summary>
    /// Cabeceras de seguridad en todas las respuestas, también en las de error.
    /// La API solo devuelve JSON: nada se puede incrustar, ejecutar ni cargar desde ella.
    /// </summary>
    public class SecurityHeadersMiddleware(RequestDelegate next)
    {
        public Task Invoke(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";

            // Swagger UI (solo en desarrollo) necesita cargar sus propios scripts y estilos.
            if (!context.Request.Path.StartsWithSegments("/swagger"))
                headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

            return next(context);
        }
    }
}
