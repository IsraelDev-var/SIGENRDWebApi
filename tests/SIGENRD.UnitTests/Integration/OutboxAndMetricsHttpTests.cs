using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SIGENRD.Core.Application.Diagnostics;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Infrastructure.Shared.Services;

namespace SIGENRD.UnitTests.Integration;

/// <summary>SCA-01 y OPS-02 a través de la API real: el correo va a la bandeja y los inicios de sesión se miden.</summary>
[Collection(ApiCollection.Name)]
public class OutboxAndMetricsHttpTests(ApiFactory api)
{
    [FactIfDatabase]
    public async Task Con_el_SMTP_caido_el_registro_responde_bien_y_el_correo_queda_en_la_bandeja()
    {
        // La API con la bandeja de salida real (las demás pruebas capturan los correos en memoria).
        // El despachador está apagado en las pruebas: aunque el SMTP no exista, la petición no lo toca.
        using var withOutbox = api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService, OutboxEmailService>();
        }));
        var email = "bandeja-http-" + Guid.NewGuid().ToString("N")[..8] + "@test.energymap.local";

        var response = await withOutbox.CreateClient().PostAsJsonAsync("/api/v1/Auth/register",
            new { fullName = "Con bandeja", email, password = ApiFactory.TestPassword, userType = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = api.NewAppContext();
        var queued = await db.OutboxEmails.AsNoTracking().SingleAsync(e => e.To == email);
        Assert.Null(queued.SentAt);
        Assert.Contains("confirm-email", queued.Body);
    }

    [FactIfDatabase]
    public async Task Los_inicios_de_sesion_se_cuentan_por_resultado()
    {
        var measurements = new System.Collections.Concurrent.ConcurrentBag<(long Value, string? Result)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == EnergyMapMetrics.MeterName && instrument.Name == "energymap.auth.logins")
                    l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            string? result = null;
            foreach (var tag in tags)
                if (tag.Key == "result") result = tag.Value?.ToString();
            measurements.Add((value, result));
        });
        listener.Start();

        var client = api.CreateClient();
        await client.PostAsJsonAsync("/api/v1/Auth/login", new { email = ApiFactory.Email(TestRole.Engineer), password = ApiFactory.TestPassword });
        await client.PostAsJsonAsync("/api/v1/Auth/login", new { email = "nadie-metricas@test.energymap.local", password = "Incorrecta-12345" });

        Assert.Contains(measurements, m => m.Result == "success");
        Assert.Contains(measurements, m => m.Result == "failed");
    }
}
