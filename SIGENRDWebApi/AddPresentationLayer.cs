using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SIGENRDWebApi.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models; // 👈 NECESARIO PARA OpenApiSecurityScheme
using SIGENRDWebApi.Options;

namespace SIGENRD.Presentation.WebApi
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationLayer(this IServiceCollection services, IConfiguration configuration)
        {
            // El mapa es público e igual para todos: navegadores y CDN pueden guardarlo lo mismo que la caché del servidor.
            var mapCacheSeconds = configuration.GetValue("MapCache:TtlSeconds", 30);
            services.AddControllers(options => options.CacheProfiles.Add(CacheProfiles.Map, new CacheProfile
            {
                Duration = mapCacheSeconds,
                Location = ResponseCacheLocation.Any
            }))
                // Enums por nombre en el JSON y en Swagger ("Critical", no 3). Se siguen aceptando números
                // al recibir para no romper a los clientes que ya los envían.
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            // Usuario actual para la auditoría
            services.AddHttpContextAccessor();
            services.AddScoped<SIGENRD.Core.Domain.Interfaces.ICurrentUserService, SIGENRDWebApi.Services.CurrentUserService>();

            services.AddApiVersioningWithSwagger();

            services.AddCorsFromConfiguration(configuration);

            // Supuestos de la simulación de medición neta (sección "Simulation"; sin ella, los valores por defecto).
            services.Configure<SIGENRD.Core.Application.Features.Simulations.SimulationSettings>(
                configuration.GetSection(SIGENRD.Core.Application.Features.Simulations.SimulationSettings.SectionName));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<SIGENRD.Core.Application.Features.Simulations.SimulationSettings>>().Value);
            services.AddRateLimiting(configuration);

            services.AddHsts(options =>
            {
                options.MaxAge = TimeSpan.FromDays(365);
                options.IncludeSubDomains = true;
            });

            return services;
        }

        /// <summary>Solo los orígenes listados en Cors:AllowedOrigins reciben cabeceras CORS.</summary>
        public static void AddCorsFromConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsSettings.PolicyName, policy =>
                    policy.WithOrigins(settings.AllowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod());
            });
        }

        /// <summary>
        /// Límite por IP en los endpoints anónimos sensibles; al superarlo responde 429.
        /// Los límites se leen de IOptions al crear cada partición, así que se ajustan por configuración.
        /// </summary>
        public static void AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RateLimitSettings>(configuration.GetSection(RateLimitSettings.SectionName));

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(RateLimitPolicies.Auth, context =>
                    PerIp(context, settings => settings.AuthPermitLimit));

                options.AddPolicy(RateLimitPolicies.PublicTracking, context =>
                    PerIp(context, settings => settings.PublicTrackingPermitLimit));
            });
        }

        private static RateLimitPartition<string> PerIp(HttpContext context, Func<RateLimitSettings, int> permitLimit)
        {
            var settings = context.RequestServices.GetRequiredService<IOptions<RateLimitSettings>>().Value;
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit(settings),
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0
            });
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