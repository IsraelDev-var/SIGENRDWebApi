using Asp.Versioning;
using Microsoft.OpenApi.Models; // 👈 NECESARIO PARA OpenApiSecurityScheme
using SIGENRDWebApi.Options;

namespace SIGENRD.Presentation.WebApi
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationLayer(this IServiceCollection services)
        {
            services.AddControllers();

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

        public static void AddApiVersioningWithSwagger(this IServiceCollection services)
        {
            // 1. Agregar Versionamiento
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
                config.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            // 2. Agregar ApiExplorer
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // 3. Registrar la configuración de versiones (Título, descripción, etc.)
            services.ConfigureOptions<ConfigureSwaggerOptions>();

            // 4. Agregar Generador de Swagger CON SEGURIDAD JWT 🔒
            services.AddSwaggerGen(options =>
            {
                // A) Definimos el esquema de seguridad (El botón "Authorize")
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Description = "Ingrese su token JWT en este formato: Bearer {tu_token_aqui}"
                });

                // B) Aplicamos la seguridad a todos los endpoints
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });
        }
    }
}