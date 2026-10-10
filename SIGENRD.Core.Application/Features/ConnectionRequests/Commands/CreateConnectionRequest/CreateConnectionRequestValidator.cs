using FluentValidation;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Rules;

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

            // ---- Formulario de solicitud ----

            RuleFor(p => p.DeclarationAccepted)
                .Equal(true).WithMessage("El solicitante debe certificar que la información es correcta.");

            RuleFor(p => p.PropertyRelation)
                .IsInEnum().WithMessage("Relación con la propiedad inválida.");

            When(p => p.PropertyRelation != PropertyRelation.Owner, () =>
            {
                RuleFor(p => p.OwnerFullName).NotEmpty().WithMessage("Indica el nombre del dueño del inmueble.");
                RuleFor(p => p.OwnerNationalId).NotEmpty().WithMessage("Indica la cédula del dueño del inmueble.");
            });

            // Medición neta y proyectos por encima de 10 kVA exigen un ingeniero responsable.
            When(p => p.RequestType == RequestType.NetMetering || p.RequestedCapacityKva > RequiredDocuments.DirectContractMaxKva, () =>
            {
                RuleFor(p => p.EngineerFullName).NotEmpty().WithMessage("Indica el nombre del ingeniero responsable.");
                RuleFor(p => p.EngineerCodiaNumber).NotEmpty().WithMessage("Indica el número de colegiatura CODIA del ingeniero.");
            });

            RuleFor(p => p.EngineerEmail).EmailAddress().When(p => !string.IsNullOrWhiteSpace(p.EngineerEmail))
                .WithMessage("El correo del ingeniero no es válido.");

            When(p => p.RequestType == RequestType.NetMetering, () =>
            {
                RuleFor(p => p.ServiceContractNumber).NotEmpty().WithMessage("Indica el número de contrato del servicio.");
                RuleFor(p => p.MeterNumber).NotEmpty().WithMessage("Indica el número del medidor.");

                RuleFor(p => p.NetMetering)
                    .NotNull().WithMessage("La medición neta requiere los datos del sistema de generación.")
                    .SetValidator(new NetMeteringDetailsValidator()!);
            });

            When(p => p.RequestType == RequestType.Connection, () =>
            {
                RuleFor(p => p.NetMetering)
                    .Null().WithMessage("Los datos de generación solo aplican a la medición neta.");
            });
        }
    }

    public class NetMeteringDetailsValidator : AbstractValidator<NetMeteringDetailsInput>
    {
        public NetMeteringDetailsValidator()
        {
            RuleFor(n => n.GenerationSystemType).IsInEnum().WithMessage("Tipo de sistema de generación inválido.");

            RuleFor(n => n.InstalledCapacityKw)
                .GreaterThan(0).WithMessage("La capacidad instalada en kW debe ser mayor que cero.");

            RuleFor(n => n.Comments).MaximumLength(500).WithMessage("Los comentarios no pueden exceder los 500 caracteres.");

            RuleFor(n => n.Generators)
                .NotEmpty().WithMessage("Indica al menos un generador o inversor.");

            RuleForEach(n => n.Generators).SetValidator(new GeneratorInputValidator());
        }
    }

    public class GeneratorInputValidator : AbstractValidator<GeneratorInput>
    {
        public GeneratorInputValidator()
        {
            RuleFor(g => g.Manufacturer).NotEmpty().WithMessage("El fabricante del equipo es obligatorio.");
            RuleFor(g => g.GeneratorKind).IsInEnum().WithMessage("Tipo de generador inválido.");
            RuleFor(g => g.GenerationSystemType).IsInEnum().WithMessage("Tecnología de generación inválida.");

            RuleFor(g => g.Phases)
                .Must(p => p is 1 or 3).WithMessage("La conexión debe ser monofásica (1) o trifásica (3).");

            RuleFor(g => g.RatedPowerKva).GreaterThan(0).WithMessage("La potencia nominal en kVA debe ser mayor que cero.");
            RuleFor(g => g.ConnectionVoltage).GreaterThan(0).WithMessage("El voltaje de interconexión debe ser mayor que cero.");
            RuleFor(g => g.NominalCurrent).GreaterThan(0).WithMessage("La corriente nominal debe ser mayor que cero.");

            RuleFor(g => g.PowerFactorMin).InclusiveBetween(0.01m, 1m).WithMessage("El factor de potencia mínimo debe estar entre 0,01 y 1.");
            RuleFor(g => g.PowerFactorMax).InclusiveBetween(0.01m, 1m).WithMessage("El factor de potencia máximo debe estar entre 0,01 y 1.");
            RuleFor(g => g).Must(g => g.PowerFactorMin <= g.PowerFactorMax)
                .WithMessage("El factor de potencia mínimo no puede superar al máximo.");

            RuleFor(g => g.HarmonicDistortionPercent).InclusiveBetween(0, 100)
                .WithMessage("La distorsión armónica debe estar entre 0 y 100 %.");
        }
    }
}
