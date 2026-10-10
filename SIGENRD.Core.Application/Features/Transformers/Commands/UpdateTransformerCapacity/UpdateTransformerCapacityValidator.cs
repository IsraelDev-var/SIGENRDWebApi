using FluentValidation;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.UpdateTransformerCapacity
{
    public class UpdateTransformerCapacityValidator : AbstractValidator<UpdateTransformerCapacityCommand>
    {
        public UpdateTransformerCapacityValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0).WithMessage("El identificador del transformador no es válido.");

            RuleFor(c => c.TotalCapacityKva)
                .GreaterThan(0).WithMessage("La capacidad total debe ser mayor que cero.");

            RuleFor(c => c.AvailableCapacityKva)
                .GreaterThanOrEqualTo(0).WithMessage("La capacidad disponible no puede ser negativa.")
                .LessThanOrEqualTo(c => c.TotalCapacityKva).WithMessage("La capacidad disponible no puede superar la total.");
        }
    }
}
