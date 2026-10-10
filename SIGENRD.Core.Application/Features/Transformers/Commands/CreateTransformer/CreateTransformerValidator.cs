using FluentValidation;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.CreateTransformer
{
    public class CreateTransformerValidator : AbstractValidator<CreateTransformerCommand>
    {
        public CreateTransformerValidator()
        {
            RuleFor(c => c.Code)
                .NotEmpty().WithMessage("El código del transformador es obligatorio.")
                .MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");

            RuleFor(c => c.DistributorId).GreaterThan(0).WithMessage("La distribuidora es obligatoria.");
            RuleFor(c => c.ServiceZoneId).GreaterThan(0).WithMessage("La zona es obligatoria.");

            RuleFor(c => c.TotalCapacityKva)
                .GreaterThan(0).WithMessage("La capacidad total debe ser mayor que cero.");

            RuleFor(c => c.AvailableCapacityKva)
                .GreaterThanOrEqualTo(0).WithMessage("La capacidad disponible no puede ser negativa.")
                .LessThanOrEqualTo(c => c.TotalCapacityKva).WithMessage("La capacidad disponible no puede superar la total.");

            RuleFor(c => c.Latitude).InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90.");
            RuleFor(c => c.Longitude).InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180.");
        }
    }
}
