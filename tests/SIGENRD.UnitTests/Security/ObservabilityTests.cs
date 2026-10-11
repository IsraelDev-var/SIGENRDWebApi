using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog.Events;
using SIGENRDWebApi.Observability;

namespace SIGENRD.UnitTests.Security;

/// <summary>OPS-02: OpenTelemetry se activa solo con destino configurado y las sondas de health no ensucian los logs.</summary>
public class ObservabilityTests
{
    private static ServiceProvider Build(string? endpoint)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint
        }).Build();

        return new ServiceCollection().AddLogging().AddObservability(configuration).BuildServiceProvider();
    }

    [Fact]
    public void Sin_destino_OTLP_no_se_instala_OpenTelemetry()
    {
        using var services = Build(endpoint: null);

        Assert.Null(services.GetService<TracerProvider>());
        Assert.Null(services.GetService<MeterProvider>());
    }

    [Fact]
    public void Con_destino_OTLP_se_instalan_trazas_y_metricas()
    {
        using var services = Build(endpoint: "http://localhost:4317");

        Assert.NotNull(services.GetService<TracerProvider>());
        Assert.NotNull(services.GetService<MeterProvider>());
    }

    private static HttpContext Request(string path, int status)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = status;
        return context;
    }

    [Theory]
    [InlineData("/health", 200, LogEventLevel.Verbose)]
    [InlineData("/health/live", 200, LogEventLevel.Verbose)]
    [InlineData("/health", 503, LogEventLevel.Error)]
    [InlineData("/api/v1/Transformers", 200, LogEventLevel.Information)]
    [InlineData("/api/v1/Transformers", 500, LogEventLevel.Error)]
    public void Nivel_de_log_por_peticion(string path, int status, LogEventLevel expected)
    {
        Assert.Equal(expected, ObservabilityExtensions.RequestLogLevel(Request(path, status), 1, null));
    }
}
