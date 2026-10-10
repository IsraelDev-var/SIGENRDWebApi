using FluentValidation;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds
{
    public class GetTransformersInBoundsValidator : AbstractValidator<GetTransformersInBoundsQuery>
    {
        public GetTransformersInBoundsValidator()
        {
            RuleFor(q => q.MinLatitude).InclusiveBetween(-90, 90).WithMessage("La latitud mínima debe estar entre -90 y 90.");
            RuleFor(q => q.MaxLatitude).InclusiveBetween(-90, 90).WithMessage("La latitud máxima debe estar entre -90 y 90.");
            RuleFor(q => q.MinLongitude).InclusiveBetween(-180, 180).WithMessage("La longitud mínima debe estar entre -180 y 180.");
            RuleFor(q => q.MaxLongitude).InclusiveBetween(-180, 180).WithMessage("La longitud máxima debe estar entre -180 y 180.");

            RuleFor(q => q)
                .Must(q => q.MinLatitude < q.MaxLatitude && q.MinLongitude < q.MaxLongitude)
                .WithMessage("El área solicitada no es válida: los mínimos deben ser menores que los máximos.");

            RuleFor(q => q.Limit)
                .InclusiveBetween(1, GetTransformersInBoundsQuery.MaxLimit)
                .WithMessage("El límite debe estar entre 1 y " + GetTransformersInBoundsQuery.MaxLimit + ".");

            RuleFor(q => q.MinAvailabilityPct)
                .InclusiveBetween(0m, 100m).When(q => q.MinAvailabilityPct.HasValue)
                .WithMessage("La disponibilidad mínima debe estar entre 0 y 100.");

            RuleFor(q => q.Status)
                .IsInEnum().When(q => q.Status.HasValue)
                .WithMessage("Estado de transformador inválido.");
        }
    }
}
