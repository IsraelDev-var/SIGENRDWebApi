using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>Simulación de medición neta por la API real (RF-18 a RF-26, CA-10) con la consulta espacial de PostGIS.</summary>
[Collection(ApiCollection.Name)]
public class SimulationsTests(ApiFactory api)
{
    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Dos transformadores en un punto propio de la prueba (lejos de los de otras pruebas): uno a ~100 m y otro a ~600 m.
    /// </summary>
    private async Task<(double Lat, double Lng, Transformer Near, Transformer Far)> SeedAreaAsync()
    {
        var lat = 19.70 + Random.Shared.NextDouble() * 0.2;
        var lng = -71.60 + Random.Shared.NextDouble() * 0.2;

        await using var context = api.NewAppContext();
        var distributor = await context.Distributors.FirstOrDefaultAsync() ?? context.Distributors.Add(new Distributor { Name = "Edesur" }).Entity;
        var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona sim " + Unique() }).Entity;
        await context.SaveChangesAsync();

        Transformer Make(string code, double dLat, decimal available)
        {
            var t = new Transformer
            {
                Code = code,
                DistributorId = distributor.Id,
                ServiceZoneId = zone.Id,
                Location = TransformerQueries.ToPoint(lat + dLat, lng)
            };
            t.UpdateCapacity(100, available, 70, 30);
            context.Transformers.Add(t);
            return t;
        }

        var near = Make("TR-SIM-N-" + Unique(), 0.0009, 80);  // ~100 m
        var far = Make("TR-SIM-F-" + Unique(), 0.0054, 90);   // ~600 m
        await context.SaveChangesAsync();
        return (lat, lng, near, far);
    }

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, response.StatusCode + ": " + text);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("data").Clone();
    }

    private async Task<JsonElement> CreateAsync(TestRole role, object body)
        => await Data(await (await api.ClientFor(role)).PostAsJsonAsync("/api/v1/Simulations", body));

    [FactIfDatabase]
    public async Task Crea_la_simulacion_con_transformadores_de_referencia_y_sin_reservar_capacidad()
    {
        var area = await SeedAreaAsync();

        var sim = await CreateAsync(TestRole.Customer, new
        {
            name = "Casa 2 kWp",
            monthlyConsumptionKwh = 400,
            latitude = area.Lat,
            longitude = area.Lng,
            installedCapacityKw = 2
        });

        // Mismos números que el caso calculado a mano de la fórmula.
        Assert.Equal(29406m, sim.GetProperty("annualSavings").GetDecimal());
        Assert.Equal(4.08m, sim.GetProperty("paybackYears").GetDecimal());
        Assert.Equal("Profitable", sim.GetProperty("viabilityStatus").GetString());
        Assert.Equal("v1", sim.GetProperty("formulaVersion").GetString());
        Assert.Equal(1500m, sim.GetProperty("assumptions").GetProperty("specificYieldKwhPerKwp").GetDecimal());
        Assert.NotEmpty(sim.GetProperty("recommendations").EnumerateArray());
        Assert.Contains("informativa", sim.GetProperty("disclaimer").GetString());

        // RF-23: el más cercano es la referencia; la lista va del más cercano al más lejano.
        Assert.Equal(area.Near.Code, sim.GetProperty("referenceTransformer").GetProperty("code").GetString());
        var nearby = sim.GetProperty("nearbyTransformers").EnumerateArray().ToList();
        Assert.Equal(area.Near.Code, nearby[0].GetProperty("code").GetString());
        Assert.Contains(nearby, t => t.GetProperty("code").GetString() == area.Far.Code);
        Assert.InRange(nearby[0].GetProperty("distanceMeters").GetDouble(), 80, 120);
        var distances = nearby.Select(t => t.GetProperty("distanceMeters").GetDouble()).ToList();
        Assert.Equal(distances.OrderBy(d => d), distances);

        // No reserva capacidad.
        await using var context = api.NewAppContext();
        var transformer = await context.Transformers.AsNoTracking().SingleAsync(t => t.Id == area.Near.Id);
        Assert.Equal(0m, transformer.ReservedCapacityKva);
        var stored = await context.NetMeteringSimulations.AsNoTracking().SingleAsync(s => s.Id == sim.GetProperty("id").GetInt32());
        Assert.Equal(area.Near.Id, stored.TransformerId);
        Assert.NotEmpty(stored.Recommendations);
    }

    [FactIfDatabase]
    public async Task Con_equipos_la_potencia_sale_de_los_paneles_y_revisa_el_inversor()
    {
        var area = await SeedAreaAsync();

        var sim = await CreateAsync(TestRole.InstallerCompany, new
        {
            monthlyConsumptionKwh = 700,
            latitude = area.Lat,
            longitude = area.Lng,
            equipment = new object[]
            {
                new { panelModel = "Panel 550 W", ratedPowerKw = 0.55, quantity = 10 },
                new { inverterModel = "Inversor 3 kW", ratedPowerKw = 3, quantity = 1 }
            }
        });

        Assert.Equal(5.5m, sim.GetProperty("installedCapacityKw").GetDecimal());
        Assert.Equal(2, sim.GetProperty("equipment").GetArrayLength());
        Assert.Contains(sim.GetProperty("recommendations").EnumerateArray(), r => r.GetString()!.Contains("recortará producción"));
    }

    [TheoryIfDatabase]
    [InlineData(25.0, -69.9, 400, 2.0, false)]  // fuera de República Dominicana
    [InlineData(18.5, -69.9, 0, 2.0, false)]    // sin consumo
    [InlineData(18.5, -69.9, 400, null, false)] // sin potencia ni paneles
    [InlineData(18.5, -69.9, 400, null, true)]  // equipo eólico
    public async Task Rechaza_datos_invalidos(double lat, double lng, double consumption, double? kw, bool wind)
    {
        var body = new Dictionary<string, object?>
        {
            ["monthlyConsumptionKwh"] = consumption,
            ["latitude"] = lat,
            ["longitude"] = lng,
            ["installedCapacityKw"] = kw
        };
        if (wind) body["equipment"] = new[] { new { generationSystemType = 2, panelModel = "Turbina", ratedPowerKw = 3, quantity = 1 } };

        var response = await (await api.ClientFor(TestRole.Customer)).PostAsJsonAsync("/api/v1/Simulations", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [FactIfDatabase]
    public async Task Historial_comparacion_y_reporte_solo_con_las_propias()
    {
        var area = await SeedAreaAsync();
        var a = await CreateAsync(TestRole.Customer, new { name = "Opción A", monthlyConsumptionKwh = 400, latitude = area.Lat, longitude = area.Lng, installedCapacityKw = 2 });
        var b = await CreateAsync(TestRole.Customer, new { name = "Opción B", monthlyConsumptionKwh = 400, latitude = area.Lat, longitude = area.Lng, installedCapacityKw = 2, investmentCost = 250000 });
        var idA = a.GetProperty("id").GetInt32();
        var idB = b.GetProperty("id").GetInt32();
        var customer = await api.ClientFor(TestRole.Customer);

        // RF-26: historial propio, más recientes primero.
        var history = await Data(await customer.GetAsync("/api/v1/Simulations?pageSize=100"));
        var ids = history.GetProperty("items").EnumerateArray().Select(s => s.GetProperty("id").GetInt32()).ToList();
        Assert.True(ids.IndexOf(idB) < ids.IndexOf(idA));

        // RF-22: comparación.
        var comparison = await Data(await customer.GetAsync($"/api/v1/Simulations/compare?ids={idA}&ids={idB}"));
        Assert.Equal(2, comparison.GetProperty("simulations").GetArrayLength());
        Assert.Equal(idA, comparison.GetProperty("bestPaybackId").GetInt32());
        Assert.Equal(idA, comparison.GetProperty("bestRoiId").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await customer.GetAsync($"/api/v1/Simulations/compare?ids={idA}")).StatusCode);

        // CA-10: reporte en PDF.
        var report = await customer.GetAsync("/api/v1/Simulations/" + idA + "/report");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        Assert.Equal("application/pdf", report.Content.Headers.ContentType!.MediaType);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString((await report.Content.ReadAsByteArrayAsync())[..5]));

        // Las de otro usuario no se ven, ni se comparan, ni se descargan.
        var installer = await api.ClientFor(TestRole.InstallerCompany);
        Assert.Equal(HttpStatusCode.NotFound, (await installer.GetAsync("/api/v1/Simulations/" + idA)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await installer.GetAsync("/api/v1/Simulations/" + idA + "/report")).StatusCode);
        var mine = await CreateAsync(TestRole.InstallerCompany, new { monthlyConsumptionKwh = 300, latitude = area.Lat, longitude = area.Lng, installedCapacityKw = 1 });
        Assert.Equal(HttpStatusCode.NotFound,
            (await installer.GetAsync($"/api/v1/Simulations/compare?ids={mine.GetProperty("id").GetInt32()}&ids={idA}")).StatusCode);
        var installerHistory = await Data(await installer.GetAsync("/api/v1/Simulations?pageSize=100"));
        Assert.DoesNotContain(installerHistory.GetProperty("items").EnumerateArray(), s => s.GetProperty("id").GetInt32() == idA);
    }

    [FactIfDatabase]
    public async Task Solo_clientes_e_instaladoras_simulan()
    {
        // Matriz de permisos 3.2: el personal, incluido el administrador, no crea simulaciones.
        var body = new { monthlyConsumptionKwh = 400, latitude = 18.5, longitude = -69.9, installedCapacityKw = 2 };

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsJsonAsync("/api/v1/Simulations", body)).StatusCode);
        foreach (var role in new[] { TestRole.Engineer, TestRole.Distributor, TestRole.Admin })
        {
            var client = await api.ClientFor(role);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/Simulations", body)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/Simulations")).StatusCode);
        }
    }
}
