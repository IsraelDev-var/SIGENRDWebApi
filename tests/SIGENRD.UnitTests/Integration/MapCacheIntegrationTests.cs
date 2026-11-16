using SIGENRD.Infrastructure.Persistences.Seeds;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules.Simulation;
using SIGENRD.Infrastructure.Persistences.Caching;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Integration;

/// <summary>La caché del mapa contra PostGIS: da lo mismo que la consulta espacial y se invalida tras el commit.</summary>
public class MapCacheEquivalenceTests(PostgisFixture db) : IClassFixture<PostgisFixture>
{
    [FactIfDatabase]
    public async Task La_copia_en_memoria_da_los_mismos_resultados_que_PostGIS()
    {
        // 400 transformadores alrededor de un punto propio de esta prueba.
        var random = new Random(7);
        await using (var context = db.NewContext())
        {
            for (var i = 0; i < 400; i++)
                await db.AddTransformer(context, $"TR-EQ-{i:D4}", db.ZoneSantoDomingoId,
                    19.0 + (random.NextDouble() - 0.5) * 0.2, -71.0 + (random.NextDouble() - 0.5) * 0.2,
                    1000, (decimal)Math.Round(random.NextDouble() * 1000, 2));
            await context.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        var repository = new TransformerRepository(read);
        var all = await read.Transformers.AsNoTracking().ToListAsync();
        all.Sort((a, b) => string.CompareOrdinal(a.Code, b.Code));
        var snapshot = new TransformerMapSnapshot(all, DateTimeOffset.UtcNow);

        var filters = new[]
        {
            new TransformerFilter(),
            new TransformerFilter(Status: Core.Domain.Enums.TransformerStatus.Available),
            new TransformerFilter(MinAvailabilityPct: 50),
            new TransformerFilter(ServiceZoneId: db.ZoneSantoDomingoId)
        };

        for (var i = 0; i < 25; i++)
        {
            var lat = 19.0 + (random.NextDouble() - 0.5) * 0.15;
            var lng = -71.0 + (random.NextDouble() - 0.5) * 0.15;
            var span = 0.01 + random.NextDouble() * 0.1;
            var filter = filters[i % filters.Length];
            var limit = i % 3 == 0 ? 15 : 500;

            // Encuadre: mismos transformadores y mismo orden.
            var expected = await repository.GetInBoundsAsync(lat - span, lng - span, lat + span, lng + span, filter, limit);
            var actual = snapshot.InBounds(lat - span, lng - span, lat + span, lng + span, filter, limit);
            Assert.Equal(expected.Select(t => t.Id), actual.Select(t => t.Id));

            // Cercanía: PostGIS mide sobre el elipsoide y la copia sobre la esfera (difieren en torno al 0,3 %);
            // se comparan dejando fuera los que están justo en el borde del radio.
            var radius = 500 + random.NextDouble() * 4000;
            var dbNear = await repository.GetNearAsync(lat, lng, radius, filter, 1000);
            var memNear = snapshot.Near(lat, lng, radius, filter, 1000);
            bool Clear(Transformer t) => Math.Abs(Geo.DistanceMeters(lat, lng, t.Location.Y, t.Location.X) - radius) > radius * 0.01;
            Assert.Equal(dbNear.Where(Clear).Select(t => t.Id).ToHashSet(), memNear.Where(Clear).Select(t => t.Id).ToHashSet());
        }
    }

    [FactIfDatabase]
    public async Task Dentro_de_una_transaccion_invalida_solo_al_confirmar()
    {
        var services = new ServiceCollection()
            .AddSingleton(new MapCacheSettings())
            .AddSingleton(TimeProvider.System)
            .AddSingleton<TransformerMapCache>()
            .AddSingleton<TransformerChangeInterceptor>()
            .BuildServiceProvider();
        var cache = services.GetRequiredService<TransformerMapCache>();
        var interceptor = services.GetRequiredService<TransformerChangeInterceptor>();

        AppContextSIGENRD Context() => new(new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql(db.ConnectionString, n => n.UseNetTopologySuite())
            .AddInterceptors(interceptor).Options, new FakeCurrentUser(PostgisFixture.UserId));

        int id;
        await using (var context = Context())
        {
            var t = await db.AddTransformer(context, "TR-TX-" + Guid.NewGuid().ToString("N")[..6], db.ZoneSantoDomingoId, 18.9, -70.9, 100, 90);
            await context.SaveChangesAsync();
            id = t.Id;
        }

        // Revertida: no invalida.
        var version = cache.Version;
        await using (var context = Context())
        {
            await using var tx = await context.Database.BeginTransactionAsync();
            (await context.Transformers.SingleAsync(t => t.Id == id)).UpdateCapacity(100, 10, 70, 30);
            await context.SaveChangesAsync();
            Assert.Equal(version, cache.Version); // guardado pero sin confirmar
            await tx.RollbackAsync();
        }
        Assert.Equal(version, cache.Version);

        // Confirmada: invalida al confirmar, no antes.
        await using (var context = Context())
        {
            await using var tx = await context.Database.BeginTransactionAsync();
            (await context.Transformers.SingleAsync(t => t.Id == id)).UpdateCapacity(100, 20, 70, 30);
            await context.SaveChangesAsync();
            Assert.Equal(version, cache.Version);
            await tx.CommitAsync();
        }
        Assert.Equal(version + 1, cache.Version);

        // Sin transacción explícita: el guardado con auditoría abre la suya (DB-01) y también invalida al confirmar.
        await using (var context = Context())
        {
            (await context.Transformers.SingleAsync(t => t.Id == id)).UpdateCapacity(100, 30, 70, 30);
            await context.SaveChangesAsync();
        }
        Assert.Equal(version + 2, cache.Version);
    }
}

/// <summary>La caché del mapa por la API real: cabecera Cache-Control y cambios visibles al instante.</summary>
[Collection(ApiCollection.Name)]
public class MapCacheApiTests(ApiFactory api)
{
    private static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, response.StatusCode + ": " + text);
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.GetProperty("data").Clone();
    }

    [FactIfDatabase]
    public async Task El_mapa_se_puede_guardar_en_cache_HTTP()
    {
        var response = await api.CreateClient().GetAsync("/api/v1/Transformers?MinLatitude=18.4&MaxLatitude=18.6&MinLongitude=-70&MaxLongitude=-69.8");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.CacheControl.MaxAge);
    }

    [FactIfDatabase]
    public async Task Un_cambio_de_capacidad_se_ve_en_el_mapa_al_instante()
    {
        // Un transformador en un punto propio, creado por la API (la caché se entera).
        var code = "TR-CACHE-" + Guid.NewGuid().ToString("N")[..6];
        var lat = 17.6 + Random.Shared.NextDouble() * 0.05;
        int distributorId, zoneId;
        await using (var context = api.NewAppContext())
        {
            var distributor = await context.Distributors.FirstOrDefaultAsync(d => d.Name == DevelopmentDemoData.DistributorName) ?? context.Distributors.Add(new Distributor { Name = "Edesur" }).Entity;
            var zone = context.ServiceZones.Add(new ServiceZone { ZoneName = "Zona caché " + code }).Entity;
            await context.SaveChangesAsync();
            distributorId = distributor.Id;
            zoneId = zone.Id;
        }

        var distributorClient = await api.ClientFor(TestRole.Distributor);
        var created = await Data(await distributorClient.PostAsync("/api/v1/Transformers", new StringContent(
            $$"""{"code":"{{code}}","distributorId":{{distributorId}},"serviceZoneId":{{zoneId}},"totalCapacityKva":100,"availableCapacityKva":90,"latitude":{{lat}},"longitude":-71.0}""",
            Encoding.UTF8, "application/json")));
        var id = created.GetInt32();

        var url = $"/api/v1/Transformers?MinLatitude={lat - 0.001}&MaxLatitude={lat + 0.001}&MinLongitude=-71.001&MaxLongitude=-70.999";
        var anonymous = api.CreateClient();
        var before = (await Data(await anonymous.GetAsync(url))).GetProperty("items").EnumerateArray().Single(t => t.GetProperty("id").GetInt32() == id);
        Assert.Equal(90m, before.GetProperty("availabilityPct").GetDecimal());

        var update = await distributorClient.PutAsync($"/api/v1/Transformers/{id}/capacity",
            new StringContent("""{"totalCapacityKva":100,"availableCapacityKva":15}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var after = (await Data(await anonymous.GetAsync(url))).GetProperty("items").EnumerateArray().Single(t => t.GetProperty("id").GetInt32() == id);
        Assert.Equal(15m, after.GetProperty("availabilityPct").GetDecimal());
        Assert.Equal("Critical", after.GetProperty("status").GetString());
    }
}
