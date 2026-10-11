using FluentValidation;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Behaviours;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;
using AppValidationException = SIGENRD.Core.Application.Exceptions.ValidationException;

namespace SIGENRD.UnitTests.Application;

/// <summary>
/// El middleware de errores traduce a 400 solo la excepción propia de la aplicación.
/// Si el pipeline lanzara la de FluentValidation, el cliente recibiría un 500.
/// </summary>
public class ValidationBehaviorTests
{
    [Fact]
    public async Task Una_peticion_invalida_lanza_la_excepcion_de_validacion_de_la_aplicacion()
    {
        var behavior = new ValidationBehavior<GetTransformersInBoundsQuery, Response<TransformerMapResultDto>>(
            new IValidator<GetTransformersInBoundsQuery>[] { new GetTransformersInBoundsValidator() });

        var invalid = new GetTransformersInBoundsQuery(); // todo en cero: el área no es válida

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(invalid, _ => Task.FromResult(new Response<TransformerMapResultDto>()), default));

        Assert.NotEmpty(ex.Errors);
    }

    [Fact]
    public async Task Una_peticion_valida_continua_el_pipeline()
    {
        var behavior = new ValidationBehavior<GetTransformersInBoundsQuery, Response<TransformerMapResultDto>>(
            new IValidator<GetTransformersInBoundsQuery>[] { new GetTransformersInBoundsValidator() });

        var valid = new GetTransformersInBoundsQuery { MinLatitude = 18, MaxLatitude = 19, MinLongitude = -70, MaxLongitude = -69 };
        var called = false;

        await behavior.Handle(valid, _ => { called = true; return Task.FromResult(new Response<TransformerMapResultDto>()); }, default);

        Assert.True(called);
    }
}
