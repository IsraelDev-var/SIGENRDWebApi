using Asp.Versioning;

using SIGENRDWebApi.Options; // Namespace donde pusiste la clase SwaggerOptions del paso 1

namespace SIGENRD.Presentation.WebApi
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
        {
            services.AddControllers();

            // Aquí llamamos a nuestro método personalizado
            services.AddApiVersioningWithSwagger();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });

            return services;
        }

        // 👇 ESTA ES LA DEFINICIÓN QUE TE FALTABA
        public static void AddApiVersioningWithSwagger(this IServiceCollection services)
        {
            // 1. Agregar Versionamiento
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
                // Usar versionamiento por URL (ej: /api/v1/producto)
                config.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            // 2. Agregar ApiExplorer (Necesario para que Swagger detecte las versiones)
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Formato v1, v2, etc.
                options.SubstituteApiVersionInUrl = true;
            });

            // 3. Registrar la configuración de opciones de Swagger creada en el Paso 1
            services.ConfigureOptions<ConfigureSwaggerOptions>();

            // 4. Agregar Generador de Swagger
            services.AddSwaggerGen();
        }
    }
}
