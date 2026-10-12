using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.UnitTests.Domain;

/// <summary>Agrupación de transformadores por zoom (B-2, RNF-02, RNF-23).</summary>
public class TransformerGridTests
{
    private static int _nextId = 1;

    private static Transformer At(double lat, double lng, TransformerStatus status = TransformerStatus.Available)
    {
        var id = Interlocked.Increment(ref _nextId);
        return new Transformer
        {
            Id = id,
            Code = "TR-" + id,
            Location = new Point(lng, lat) { SRID = 4326 },
            Status = status
        };
    }

    [Fact]
    public void La_celda_se_reduce_a_la_mitad_con_cada_nivel_de_zoom()
    {
        Assert.Equal(TransformerGrid.CellSizeDegrees(8) / 2, TransformerGrid.CellSizeDegrees(9), 12);
        // 64 px a zoom 0, con un mundo de 512 px de ancho: 45 grados.
        Assert.Equal(45, TransformerGrid.CellSizeDegrees(0), 12);
    }

    [Fact]
    public void Junta_los_cercanos_y_cuenta_cada_estado()
    {
        var points = new[]
        {
            At(18.4800, -69.9300, TransformerStatus.Available),
            At(18.4801, -69.9301, TransformerStatus.Critical),
            At(18.4802, -69.9302, TransformerStatus.Saturated),
            At(18.4803, -69.9303, TransformerStatus.Maintenance),
            At(18.4804, -69.9304, TransformerStatus.Conditioned),
        };

        var cluster = Assert.Single(TransformerGrid.Aggregate(points, zoom: 7));

        Assert.Equal(5, cluster.Count);
        Assert.Equal((1, 1, 1, 1, 1), (cluster.Available, cluster.Conditioned, cluster.Critical, cluster.Saturated, cluster.Maintenance));
        Assert.Equal(18.4802, cluster.Latitude, 6);
        Assert.Null(cluster.TransformerId);
    }

    [Fact]
    public void Separa_los_lejanos_y_suma_todos()
    {
        var points = Enumerable.Range(0, 20).Select(i => At(18.48, -69.93 + i * 0.001))   // Santo Domingo
            .Concat(Enumerable.Range(0, 7).Select(i => At(19.45, -70.69 + i * 0.001)))   // Santiago
            .ToList();

        var clusters = TransformerGrid.Aggregate(points, zoom: 7);

        Assert.Equal(2, clusters.Count);
        Assert.Equal(27, clusters.Sum(c => c.Count));
        // Los más poblados primero.
        Assert.Equal(20, clusters[0].Count);
    }

    [Fact]
    public void Con_mas_zoom_hay_mas_grupos()
    {
        var random = new Random(3);
        var points = Enumerable.Range(0, 500)
            .Select(_ => At(18.4 + random.NextDouble() * 0.3, -70.0 + random.NextDouble() * 0.3))
            .ToList();

        var country = TransformerGrid.Aggregate(points, zoom: 7).Count;
        var city = TransformerGrid.Aggregate(points, zoom: 12);

        Assert.True(city.Count > country, $"zoom 12 ({city.Count}) debería dar más grupos que zoom 7 ({country})");
        Assert.Equal(500, city.Sum(c => c.Count));
    }

    [Fact]
    public void Un_grupo_de_un_solo_transformador_lo_identifica()
    {
        var single = At(18.0, -71.0, TransformerStatus.Critical);

        var cluster = Assert.Single(TransformerGrid.Aggregate([single], zoom: 10));

        Assert.Equal(single.Id, cluster.TransformerId);
        Assert.Equal(single.Code, cluster.Code);
        Assert.Equal(TransformerStatus.Critical, cluster.Status);
    }

    [Fact]
    public void Sin_transformadores_no_hay_grupos()
    {
        Assert.Empty(TransformerGrid.Aggregate([], zoom: 7));
    }

    [Fact]
    public void Diez_mil_transformadores_se_agrupan_rapido()
    {
        var random = new Random(11);
        var points = Enumerable.Range(0, 10_000)
            .Select(_ => At(17.6 + random.NextDouble() * 2.2, -71.9 + random.NextDouble() * 3.5))
            .ToList();

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var clusters = TransformerGrid.Aggregate(points, zoom: 7);
        watch.Stop();

        Assert.Equal(10_000, clusters.Sum(c => c.Count));
        Assert.True(clusters.Count < 1_000, $"a escala país deben ser pocos grupos, no {clusters.Count}");
        Assert.True(watch.ElapsedMilliseconds < 500, $"tardó {watch.ElapsedMilliseconds} ms");
    }
}
