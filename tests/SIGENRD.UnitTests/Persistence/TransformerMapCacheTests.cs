using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Caching;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Persistence;

/// <summary>Caché del mapa con la base en memoria: vigencia, invalidación y una sola carga a la vez.</summary>
public class TransformerMapCacheTests
{
    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 11, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed record Harness(ServiceProvider Services, ManualTime Time, TransformerMapCache Cache) : IDisposable
    {
        public AppContextSIGENRD NewContext() => Services.CreateScope().ServiceProvider.GetRequiredService<AppContextSIGENRD>();
        public void Dispose() => Services.Dispose();
    }

    private static Harness Build(int ttlSeconds = 30)
    {
        var time = new ManualTime();
        var name = "mapa-" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection()
            .AddSingleton(new MapCacheSettings { Enabled = true, TtlSeconds = ttlSeconds })
            .AddSingleton<TimeProvider>(time)
            .AddSingleton<TransformerMapCache>()
            .AddSingleton<TransformerChangeInterceptor>()
            .AddDbContext<AppContextSIGENRD>((sp, o) => o
                .UseInMemoryDatabase(name)
                .AddInterceptors(sp.GetRequiredService<TransformerChangeInterceptor>()))
            .BuildServiceProvider();
        return new Harness(services, time, services.GetRequiredService<TransformerMapCache>());
    }

    private static Transformer Make(string code, double lat, double lng, decimal available = 80, int zone = 1)
    {
        var t = new Transformer { Code = code, DistributorId = 1, ServiceZoneId = zone, Location = TransformerQueries.ToPoint(lat, lng) };
        t.UpdateCapacity(100, available, 70, 30);
        return t;
    }

    private static async Task SeedAsync(Harness h, params Transformer[] transformers)
    {
        await using var db = h.NewContext();
        db.Transformers.AddRange(transformers);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Reutiliza_la_copia_mientras_esta_vigente_y_la_renueva_al_vencer()
    {
        using var h = Build(ttlSeconds: 30);
        await SeedAsync(h, Make("TR-1", 18.5, -69.9));

        var first = await h.Cache.GetAsync();
        h.Time.Now += TimeSpan.FromSeconds(29);
        Assert.Same(first, await h.Cache.GetAsync());

        h.Time.Now += TimeSpan.FromSeconds(2);
        Assert.NotSame(first, await h.Cache.GetAsync());
    }

    [Fact]
    public async Task Guardar_un_cambio_en_un_transformador_invalida_la_copia()
    {
        using var h = Build();
        await SeedAsync(h, Make("TR-1", 18.5, -69.9, available: 80));
        var before = await h.Cache.GetAsync();
        Assert.Equal(80m, before.InBounds(18, -70, 19, -69, new TransformerFilter(), 10).Single().AvailabilityPct);

        await using (var db = h.NewContext())
        {
            var t = await db.Transformers.SingleAsync();
            t.UpdateCapacity(100, 10, 70, 30);
            await db.SaveChangesAsync();
        }

        var after = await h.Cache.GetAsync();
        Assert.NotSame(before, after);
        var point = after.InBounds(18, -70, 19, -69, new TransformerFilter(), 10).Single();
        Assert.Equal(10m, point.AvailabilityPct);
        Assert.Equal(TransformerStatus.Critical, point.Status);
    }

    [Fact]
    public async Task Guardar_otras_entidades_no_invalida_la_copia()
    {
        using var h = Build();
        await SeedAsync(h, Make("TR-1", 18.5, -69.9));
        var version = h.Cache.Version;

        await using (var db = h.NewContext())
        {
            db.ServiceZones.Add(new ServiceZone { ZoneName = "Otra" });
            await db.SaveChangesAsync();
        }

        Assert.Equal(version, h.Cache.Version);
    }

    [Fact]
    public async Task Con_muchas_peticiones_a_la_vez_carga_una_sola_vez()
    {
        using var h = Build();
        await SeedAsync(h, Enumerable.Range(1, 500).Select(i => Make("TR-" + i.ToString("D4"), 18 + i * 0.001, -70)).ToArray());
        h.Cache.Invalidate();

        var snapshots = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => h.Cache.GetAsync().AsTask()));

        Assert.All(snapshots, s => Assert.Same(snapshots[0], s));
        Assert.Equal(500, snapshots[0].Count);
    }

    // ---------------- consultas sobre la copia ----------------

    private static TransformerMapSnapshot Snapshot(params Transformer[] transformers) =>
        new(transformers.OrderBy(t => t.Code, StringComparer.Ordinal).ToList(), DateTimeOffset.UtcNow);

    [Fact]
    public void El_encuadre_filtra_ordena_por_codigo_y_respeta_el_limite()
    {
        var snapshot = Snapshot(
            Make("TR-C", 18.50, -69.90),
            Make("TR-A", 18.51, -69.91, available: 20),
            Make("TR-B", 18.52, -69.92, zone: 2),
            Make("TR-FUERA", 19.50, -69.90));

        Assert.Equal(new[] { "TR-A", "TR-B", "TR-C" }, snapshot.InBounds(18, -70, 19, -69, new TransformerFilter(), 10).Select(t => t.Code));
        Assert.Equal(new[] { "TR-A", "TR-B" }, snapshot.InBounds(18, -70, 19, -69, new TransformerFilter(), 2).Select(t => t.Code));
        Assert.Equal(new[] { "TR-B" }, snapshot.InBounds(18, -70, 19, -69, new TransformerFilter(ServiceZoneId: 2), 10).Select(t => t.Code));
        Assert.Equal(new[] { "TR-A" }, snapshot.InBounds(18, -70, 19, -69, new TransformerFilter(Status: TransformerStatus.Critical), 10).Select(t => t.Code));
        Assert.Equal(new[] { "TR-B", "TR-C" }, snapshot.InBounds(18, -70, 19, -69, new TransformerFilter(MinAvailabilityPct: 50), 10).Select(t => t.Code));
    }

    [Fact]
    public void La_cercania_ordena_por_distancia_y_respeta_el_radio()
    {
        var snapshot = Snapshot(
            Make("TR-LEJOS", 18.5180, -69.9000),  // ~2 km
            Make("TR-CERCA", 18.5009, -69.9000),  // ~100 m
            Make("TR-MEDIO", 18.5045, -69.9000),  // ~500 m
            Make("TR-FUERA", 18.5300, -69.9000)); // ~3.3 km

        var near = snapshot.Near(18.5, -69.9, 2100, new TransformerFilter(), 10).Select(t => t.Code);

        Assert.Equal(new[] { "TR-CERCA", "TR-MEDIO", "TR-LEJOS" }, near);
        Assert.Equal(new[] { "TR-CERCA" }, snapshot.Near(18.5, -69.9, 2100, new TransformerFilter(), 1).Select(t => t.Code));
    }
}
