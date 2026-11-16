using SIGENRD.Infrastructure.Persistences.Seeds;
using System.Net;
using System.Text.Json;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>B-3: lista paginada de la red para la distribuidora, con filtros y orden.</summary>
[Collection(ApiCollection.Name)]
public class TransformerManagementTests(ApiFactory api)
{
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    /// <summary>Tres transformadores con un prefijo propio: holgado, justo y en mantenimiento, en una zona nueva.</summary>
    private async Task<(string Prefix, int ZoneId)> SeedAsync()
    {
        var prefix = "TR-MG-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant() + "-";
        await using var context = api.NewAppContext();
        var distributor = context.Distributors.FirstOrDefault(d => d.Name == DevelopmentDemoData.DistributorName) ?? context.Distributors.Add(new Distributor { Name = "Edesur" }).Entity;
        var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona " + prefix }).Entity;
        await context.SaveChangesAsync();

        Transformer Make(string suffix, decimal available, DateTime updated)
        {
            var t = new Transformer { Code = prefix + suffix, DistributorId = distributor.Id, ServiceZoneId = zone.Id, Location = TransformerQueries.ToPoint(18.4, -69.9) };
            t.UpdateCapacity(100, available, 70, 30);
            t.LastUpdated = updated;
            return t;
        }

        var maintenance = Make("C", 50, DateTime.UtcNow.AddDays(-1));
        maintenance.EnterMaintenance();
        context.Transformers.AddRange(Make("A", 90, DateTime.UtcNow.AddDays(-2)), Make("B", 10, DateTime.UtcNow.AddDays(-40)), maintenance);
        await context.SaveChangesAsync();
        return (prefix, zone.Id);
    }

    private static string[] Codes(JsonElement data) =>
        data.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("code").GetString()!).ToArray();

    [FactIfDatabase]
    public async Task Filtra_por_codigo_zona_y_estado()
    {
        var (prefix, zoneId) = await SeedAsync();
        var client = await api.ClientFor(TestRole.Distributor);

        var byCode = await Data(await client.GetAsync("/api/v1/Transformers/manage?code=" + prefix.ToLowerInvariant()));
        Assert.Equal(new[] { prefix + "A", prefix + "B", prefix + "C" }, Codes(byCode));
        Assert.Equal(3, byCode.GetProperty("totalCount").GetInt32());
        Assert.Equal("Zona " + prefix, byCode.GetProperty("items")[0].GetProperty("zoneName").GetString());

        var byStatus = await Data(await client.GetAsync("/api/v1/Transformers/manage?serviceZoneId=" + zoneId + "&status=Maintenance"));
        Assert.Equal(new[] { prefix + "C" }, Codes(byStatus));
    }

    [FactIfDatabase]
    public async Task Ordena_por_disponibilidad_y_por_antiguedad_del_dato()
    {
        var (prefix, zoneId) = await SeedAsync();
        var client = await api.ClientFor(TestRole.Admin);

        var byAvailability = await Data(await client.GetAsync("/api/v1/Transformers/manage?serviceZoneId=" + zoneId + "&sort=Availability"));
        // El de mantenimiento (C) va al final: su disponibilidad no se calcula.
        Assert.Equal(new[] { prefix + "B", prefix + "A", prefix + "C" }, Codes(byAvailability));

        var byAge = await Data(await client.GetAsync("/api/v1/Transformers/manage?serviceZoneId=" + zoneId + "&sort=LastUpdated"));
        Assert.Equal(prefix + "B", Codes(byAge)[0]);
    }

    [FactIfDatabase]
    public async Task Pagina_y_no_trata_el_texto_como_comodin()
    {
        var (prefix, zoneId) = await SeedAsync();
        var client = await api.ClientFor(TestRole.Distributor);

        var page2 = await Data(await client.GetAsync("/api/v1/Transformers/manage?serviceZoneId=" + zoneId + "&pageSize=2&page=2"));
        Assert.Equal(new[] { prefix + "C" }, Codes(page2));
        Assert.Equal(2, page2.GetProperty("totalPages").GetInt32());

        var wildcard = await Data(await client.GetAsync("/api/v1/Transformers/manage?serviceZoneId=" + zoneId + "&code=%25"));
        Assert.Equal(0, wildcard.GetProperty("totalCount").GetInt32());
    }

    [TheoryIfDatabase]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("sort=Nada")]
    [InlineData("status=99")]
    public async Task Rechaza_parametros_invalidos(string query)
    {
        var response = await (await api.ClientFor(TestRole.Admin)).GetAsync("/api/v1/Transformers/manage?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
