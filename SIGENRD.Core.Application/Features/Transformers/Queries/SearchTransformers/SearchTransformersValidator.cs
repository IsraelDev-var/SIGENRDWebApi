using FluentValidation;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.SearchTransformers
{
    public class SearchTransformersValidator : AbstractValidator<SearchTransformersQuery>
    {
        public SearchTransformersValidator()
        {
            RuleFor(q => q)
                .Must(q => !string.IsNullOrWhiteSpace(q.Code) || (q.Latitude.HasValue && q.Longitude.HasValue))
                .WithMessage("Indica un código o unas coordenadas (latitud y longitud) para buscar.");

            RuleFor(q => q)
                .Must(q => string.IsNullOrWhiteSpace(q.Code) || (!q.Latitude.HasValue && !q.Longitude.HasValue))
                .WithMessage("Busca por código o por coordenadas, no por ambos a la vez.");

            RuleFor(q => q.Code)
                .MinimumLength(2).When(q => !string.IsNullOrWhiteSpace(q.Code))
                .WithMessage("El código debe tener al menos 2 caracteres.")
                .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

            RuleFor(q => q.Latitude)
                .InclusiveBetween(-90, 90).When(q => q.Latitude.HasValue)
                .WithMessage("La latitud debe estar entre -90 y 90.");

            RuleFor(q => q.Longitude)
                .InclusiveBetween(-180, 180).When(q => q.Longitude.HasValue)
                .WithMessage("La longitud debe estar entre -180 y 180.");

            RuleFor(q => q.RadiusMeters)
                .GreaterThan(0).LessThanOrEqualTo(SearchTransformersQuery.MaxRadiusMeters)
                .WithMessage("El radio debe estar entre 1 y " + SearchTransformersQuery.MaxRadiusMeters + " metros.");

            RuleFor(q => q.Limit)
                .InclusiveBetween(1, SearchTransformersQuery.MaxLimit)
                .WithMessage("El límite debe estar entre 1 y " + SearchTransformersQuery.MaxLimit + ".");
        }
    }
}
