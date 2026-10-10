using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    public class CreateConnectionRequestValidator : AbstractValidator<CreateConnectionRequestCommand>
    {
        public CreateConnectionRequestValidator()
        {
            RuleFor(p => p.CustomerId)
                .GreaterThan(0).WithMessage("El ID del cliente es obligatorio.");

            RuleFor(p => p.InstallerCompanyId)
                .GreaterThan(0).When(p => p.InstallerCompanyId.HasValue)
                .WithMessage("La compañía instaladora no es válida.");

            RuleFor(p => p.TransformerId)
                .GreaterThan(0).WithMessage("El transformador es obligatorio (RN-08).");

            RuleFor(p => p.RequestType)
                .IsInEnum().WithMessage("Tipo de solicitud inválido.");

            RuleFor(p => p.RequestedCapacityKva)
                .GreaterThan(0).WithMessage("La capacidad requerida en kVA debe ser mayor que cero.");

            RuleFor(p => p.DistributorId)
                .GreaterThan(0).WithMessage("La distribuidora es obligatoria.");

            RuleFor(p => p.ProjectAddress)
                .NotEmpty().WithMessage("La dirección del proyecto no puede estar vacía.")
                .MaximumLength(200).WithMessage("La dirección no puede exceder los 200 caracteres.");

            RuleFor(p => p.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90.");

            RuleFor(p => p.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180.");

            RuleFor(p => p.UsageType)
                .IsInEnum().WithMessage("Tipo de uso inválido.");
        }
    }
}
