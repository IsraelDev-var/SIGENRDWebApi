using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.UnitTests.Persistence;

/// <summary>
/// Comprueba el SQL que EF genera para las consultas espaciales, sin base de datos.
/// No sustituye una prueba contra PostGIS: confirma que la consulta se traduce a funciones espaciales
/// (y no se evalúa en memoria) y que los filtros llegan al WHERE.
/// </summary>
public class TransformerQueryTests
{
    private static AppContextSIGENRD NewContext()
    {
        var options = new DbContextOptionsBuilder<AppContextSIGENRD>()
            .UseNpgsql("Host=localhost;Database=query-test", npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new AppContextSIGENRD(options);
    }

    [Fact]
    public void Area_visible_se_traduce_a_ST_Intersects_ordenado_y_limitado()
    {
        using var context = NewContext();

        var sql = context.Transformers
            .InBounds(18.4, -70.0, 18.6, -69.8, new TransformerFilter(), limit: 101)
            .ToQueryString();

        Assert.Contains("ST_Intersects", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("LIMIT", sql);
    }

    [Fact]
    public void Cercania_se_traduce_a_ST_DWithin_ordenada_por_distancia()
    {
        using var context = NewContext();

        var sql = context.Transformers
            .Near(18.4861, -69.9312, 1000, new TransformerFilter(), limit: 50)
            .ToQueryString();

        Assert.Contains("ST_DWithin", sql);
        Assert.Contains("ST_Distance", sql);
    }

    [Fact]
    public void Los_filtros_llegan_al_WHERE_y_la_disponibilidad_no_divide()
    {
        using var context = NewContext();
        var filter = new TransformerFilter(DistributorId: 1, ServiceZoneId: 2, Status: TransformerStatus.Critical, MinAvailabilityPct: 15);

        var sql = context.Transformers
            .InBounds(18.4, -70.0, 18.6, -69.8, filter, limit: 10)
            .ToQueryString();

        Assert.Contains("DistributorId", sql);
        Assert.Contains("ServiceZoneId", sql);
        Assert.Contains("Status", sql);
        Assert.Contains("ReservedCapacityKva", sql);
        Assert.DoesNotContain(" / ", sql);
    }

    [Fact]
    public void El_estado_se_compara_como_texto()
    {
        using var context = NewContext();

        var sql = context.Transformers
            .InBounds(18.4, -70.0, 18.6, -69.8, new TransformerFilter(Status: TransformerStatus.Saturated), limit: 10)
            .ToQueryString();

        Assert.Contains("'Saturated'", sql);
    }

    [Fact]
    public void El_rectangulo_se_construye_en_sentido_antihorario_y_cerrado()
    {
        var polygon = TransformerQueries.ToEnvelope(18.4, -70.0, 18.6, -69.8);

        Assert.Equal(4326, polygon.SRID);
        Assert.True(polygon.Shell.IsCCW);
        Assert.True(polygon.Shell.IsClosed);
    }

    [Fact]
    public void El_punto_usa_longitud_como_X_y_latitud_como_Y()
    {
        var point = TransformerQueries.ToPoint(18.4861, -69.9312);

        Assert.Equal(-69.9312, point.X);
        Assert.Equal(18.4861, point.Y);
        Assert.Equal(4326, point.SRID);
    }
}
