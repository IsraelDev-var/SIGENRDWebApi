using System.Net;
using System.Text.Json;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>Endpoints del mapa público de F3: catálogos (B-1) y agrupación por zoom (B-2), sin sesión.</summary>
[Collection(ApiCollection.Name)]
public class MapClustersAndCatalogsTests(ApiFactory api)
{
    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private const string Country = "minLatitude=17.4&minLongitude=-72.1&maxLatitude=20.0&maxLongitude=-68.3";

    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    /// <summary>Una zona propia con 6 transformadores en Santo Domingo (4 Disponible, 2 Crítica) y 3 en Santiago (Condicionada).</summary>
    private async Task<(int ZoneId, string ZoneName, int DistributorId)> SeedAsync()
    {
        await using var context = api.NewAppContext();
        var distributor = context.Distributors.Add(new Distributor { Name = "Dist mapa " + Unique() }).Entity;
        var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona mapa " + Unique(), Province = "Prueba" }).Entity;
        await context.SaveChangesAsync();

        void Add(double lat, double lng, decimal available)
        {
            var transformer = new Transformer
            {
                Code = "TR-MAP-" + Unique(),
                DistributorId = distributor.Id,
                ServiceZoneId = zone.Id,
                Location = TransformerQueries.ToPoint(lat, lng)
            };
            transformer.UpdateCapacity(100, available, 70, 30);
            context.Transformers.Add(transformer);
        }

        for (var i = 0; i < 6; i++) Add(18.48 + i * 0.001, -69.93, i < 4 ? 90 : 10);
        for (var i = 0; i < 3; i++) Add(19.45 + i * 0.001, -70.69, 50);
        await context.SaveChangesAsync();
        return (zone.Id, zone.ZoneName, distributor.Id);
    }

    [FactIfDatabase]
    public async Task Los_catalogos_son_publicos()
    {
        var (_, zoneName, distributorId) = await SeedAsync();
        var anonymous = await api.ClientFor(TestRole.Anonymous);

        var zones = await Data(await anonymous.GetAsync("/api/v1/ServiceZones"));
        Assert.Contains(zones.EnumerateArray(), z => z.GetProperty("zoneName").GetString() == zoneName);

        var distributors = await Data(await anonymous.GetAsync("/api/v1/Distributors"));
        Assert.Contains(distributors.EnumerateArray(), d => d.GetProperty("id").GetInt32() == distributorId);
    }

    [FactIfDatabase]
    public async Task A_escala_pais_agrupa_por_ciudad_y_cuenta_estados()
    {
        var (zoneId, _, _) = await SeedAsync();
        var anonymous = await api.ClientFor(TestRole.Anonymous);

        var result = await Data(await anonymous.GetAsync($"/api/v1/Transformers/clusters?{Country}&zoom=7&serviceZoneId={zoneId}"));
        var items = result.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(9, result.GetProperty("total").GetInt32());
        Assert.Equal(2, items.Count);
        Assert.Equal(6, items[0].GetProperty("count").GetInt32());
        Assert.Equal(4, items[0].GetProperty("available").GetInt32());
        Assert.Equal(2, items[0].GetProperty("critical").GetInt32());
        Assert.Equal(3, items[1].GetProperty("conditioned").GetInt32());
    }

    [FactIfDatabase]
    public async Task Aplica_los_mismos_filtros_que_el_mapa()
    {
        var (zoneId, _, _) = await SeedAsync();
        var anonymous = await api.ClientFor(TestRole.Anonymous);

        var critical = await Data(await anonymous.GetAsync($"/api/v1/Transformers/clusters?{Country}&zoom=7&serviceZoneId={zoneId}&status=Critical"));
        Assert.Equal(2, critical.GetProperty("total").GetInt32());

        var points = await Data(await anonymous.GetAsync($"/api/v1/Transformers?{Country}&serviceZoneId={zoneId}&status=Critical"));
        Assert.Equal(2, points.GetProperty("items").GetArrayLength());
    }

    [FactIfDatabase]
    public async Task Con_mucho_zoom_cada_transformador_va_solo_y_trae_su_id()
    {
        var (zoneId, _, _) = await SeedAsync();
        var anonymous = await api.ClientFor(TestRole.Anonymous);

        var result = await Data(await anonymous.GetAsync($"/api/v1/Transformers/clusters?{Country}&zoom=18&serviceZoneId={zoneId}"));
        var items = result.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(9, items.Count);
        Assert.All(items, c =>
        {
            Assert.Equal(1, c.GetProperty("count").GetInt32());
            Assert.True(c.GetProperty("transformerId").GetInt32() > 0);
            Assert.False(string.IsNullOrEmpty(c.GetProperty("color").GetString()));
        });
    }

    [FactIfDatabase]
    public async Task Rechaza_un_zoom_o_un_area_invalidos()
    {
        var anonymous = await api.ClientFor(TestRole.Anonymous);

        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync($"/api/v1/Transformers/clusters?{Country}&zoom=40")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await anonymous.GetAsync("/api/v1/Transformers/clusters?minLatitude=20&minLongitude=-68&maxLatitude=18&maxLongitude=-70&zoom=7")).StatusCode);
    }

    private async Task<List<string>> Requirements(string query)
    {
        var anonymous = await api.ClientFor(TestRole.Anonymous);
        var data = await Data(await anonymous.GetAsync("/api/v1/RequiredDocuments?" + query));
        return data.EnumerateArray().Select(r => r.GetProperty("code").GetString()!).ToList();
    }

    [FactIfDatabase]
    public async Task Los_requisitos_publicos_cambian_segun_el_tramite()
    {
        // Conexión hasta 10 kVA, persona física: identidad e inmueble.
        Assert.Equal(["IDENTIDAD", "INMUEBLE"], await Requirements("requestType=Connection&requestedCapacityKva=5"));

        // Empresa: además carta, RNC y documentos societarios.
        var company = await Requirements("requestType=Connection&requestedCapacityKva=5&isLegalEntity=true");
        Assert.Contains("RNC", company);
        Assert.Contains("ACTA", company);

        // Más de 10 kVA: proyecto de ingeniería.
        Assert.Contains("CODIA", await Requirements("requestType=Connection&requestedCapacityKva=12"));

        // Medición neta: CNE 2012; si no es el dueño, la autorización del dueño.
        var owner = await Requirements("requestType=NetMetering");
        var tenant = await Requirements("requestType=NetMetering&propertyRelation=Tenant");
        Assert.Contains("UNIFILAR", owner);
        Assert.DoesNotContain("DUENO", owner);
        Assert.Contains("DUENO", tenant);
    }

    [FactIfDatabase]
    public async Task Los_requisitos_rechazan_valores_invalidos()
    {
        var anonymous = await api.ClientFor(TestRole.Anonymous);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/v1/RequiredDocuments?requestType=Otro")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/v1/RequiredDocuments?requestedCapacityKva=-1")).StatusCode);
    }
}
