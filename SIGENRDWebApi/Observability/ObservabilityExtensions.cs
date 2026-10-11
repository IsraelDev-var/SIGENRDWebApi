using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SIGENRD.Core.Application.Diagnostics;

namespace SIGENRDWebApi.Observability
{
    /// <summary>
    /// Logs, trazas y métricas (OPS-02).
    /// - Logs: JSON por la salida estándar fuera de desarrollo, con TraceId/SpanId; la plataforma (Azure, Docker) los recoge.
    /// - Trazas y métricas: OpenTelemetry por OTLP, solo si se define OTEL_EXPORTER_OTLP_ENDPOINT.
    /// </summary>
    public static class ObservabilityExtensions
    {
        public const string DefaultServiceName = "energymap-api";

        public static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration logger)
        {
            logger.ReadFrom.Configuration(context.Configuration)
                  .Enrich.FromLogContext();

            if (context.HostingEnvironment.IsDevelopment())
                logger.WriteTo.Console();
            else
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
        }

        /// <summary>Las sondas de /health llegan cada pocos segundos: no se registran salvo que fallen.</summary>
        public static LogEventLevel RequestLogLevel(HttpContext context, double elapsedMs, Exception? error)
        {
            if (error is not null || context.Response.StatusCode >= 500)
                return LogEventLevel.Error;

            return context.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose : LogEventLevel.Information;
        }

        public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
        {
            // Sin destino no se instala nada: no hay costo en desarrollo ni en pruebas.
            if (string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                return services;

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(configuration["OTEL_SERVICE_NAME"] ?? DefaultServiceName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = http => !http.Request.Path.StartsWithSegments("/health"))
                    .AddHttpClientInstrumentation()
                    .AddNpgsql())
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(EnergyMapMetrics.MeterName))
                // Lee OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_HEADERS y OTEL_EXPORTER_OTLP_PROTOCOL.
                .UseOtlpExporter();

            return services;
        }
    }
}
