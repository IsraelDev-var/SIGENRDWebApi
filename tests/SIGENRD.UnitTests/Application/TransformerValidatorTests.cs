using SIGENRD.Core.Application.Features.Transformers.Commands.CreateTransformer;
using SIGENRD.Core.Application.Features.Transformers.Commands.UpdateTransformerCapacity;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds;
using SIGENRD.Core.Application.Features.Transformers.Queries.SearchTransformers;

namespace SIGENRD.UnitTests.Application;

public class TransformerValidatorTests
{
    private static GetTransformersInBoundsQuery ValidBounds() => new()
    {
        MinLatitude = 18, MaxLatitude = 19, MinLongitude = -70, MaxLongitude = -69
    };

    [Fact]
    public void Area_valida() =>
        Assert.True(new GetTransformersInBoundsValidator().Validate(ValidBounds()).IsValid);

    [Fact]
    public void Area_con_minimos_mayores_que_maximos_es_invalida()
    {
        var query = ValidBounds();
        query.MinLatitude = 20;

        Assert.False(new GetTransformersInBoundsValidator().Validate(query).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public void El_limite_del_mapa_tiene_tope(int limit)
    {
        var query = ValidBounds();
        query.Limit = limit;

        Assert.False(new GetTransformersInBoundsValidator().Validate(query).IsValid);
    }

    [Fact]
    public void Latitud_fuera_de_rango()
    {
        var query = ValidBounds();
        query.MaxLatitude = 91;

        Assert.False(new GetTransformersInBoundsValidator().Validate(query).IsValid);
    }

    [Fact]
    public void Busqueda_sin_criterio_es_invalida() =>
        Assert.False(new SearchTransformersValidator().Validate(new SearchTransformersQuery()).IsValid);

    [Fact]
    public void Busqueda_por_codigo_o_por_coordenadas_es_valida()
    {
        var validator = new SearchTransformersValidator();

        Assert.True(validator.Validate(new SearchTransformersQuery { Code = "TR-10" }).IsValid);
        Assert.True(validator.Validate(new SearchTransformersQuery { Latitude = 18.4, Longitude = -69.9 }).IsValid);
    }

    [Fact]
    public void Busqueda_por_codigo_y_coordenadas_a_la_vez_es_invalida() =>
        Assert.False(new SearchTransformersValidator()
            .Validate(new SearchTransformersQuery { Code = "TR-10", Latitude = 18.4, Longitude = -69.9 }).IsValid);

    [Fact]
    public void Busqueda_con_solo_una_coordenada_es_invalida() =>
        Assert.False(new SearchTransformersValidator().Validate(new SearchTransformersQuery { Latitude = 18.4 }).IsValid);

    [Fact]
    public void El_radio_tiene_tope()
    {
        var query = new SearchTransformersQuery { Latitude = 18.4, Longitude = -69.9, RadiusMeters = 50_000 };

        Assert.False(new SearchTransformersValidator().Validate(query).IsValid);
    }

    [Fact]
    public void Crear_exige_que_la_disponible_no_supere_la_total()
    {
        var command = new CreateTransformerCommand
        {
            Code = "TR-1", DistributorId = 1, ServiceZoneId = 1,
            TotalCapacityKva = 100, AvailableCapacityKva = 150, Latitude = 18, Longitude = -69
        };

        Assert.False(new CreateTransformerValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Actualizar_capacidad_rechaza_total_en_cero()
    {
        var command = new UpdateTransformerCapacityCommand { Id = 1, TotalCapacityKva = 0, AvailableCapacityKva = 0 };

        Assert.False(new UpdateTransformerCapacityValidator().Validate(command).IsValid);
    }
}
