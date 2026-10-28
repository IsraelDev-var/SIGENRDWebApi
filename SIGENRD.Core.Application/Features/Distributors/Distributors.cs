using FluentValidation;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Distributors
{
    // Gestión de las EDE (solo el administrador): alta, datos de contacto, umbrales de disponibilidad (RN-02),
    // factor de potencia (RN-01) y desactivación. El catálogo público está en Catalogs.

    public class DistributorDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public decimal AvailableThresholdPct { get; set; }
        public decimal ConditionedThresholdPct { get; set; }
        public decimal DefaultPowerFactor { get; set; }
        public bool IsActive { get; set; }

        public static DistributorDetailDto From(Distributor d) => new()
        {
            Id = d.Id, Name = d.Name, Type = d.Type.ToString(), ContactEmail = d.ContactEmail, ContactPhone = d.ContactPhone,
            AvailableThresholdPct = d.AvailableThresholdPct, ConditionedThresholdPct = d.ConditionedThresholdPct,
            DefaultPowerFactor = d.DefaultPowerFactor, IsActive = d.IsActive
        };
    }

    // ---------------- consultas ----------------

    /// <summary>Todas las EDE, también las desactivadas.</summary>
    public record ListDistributorsForManagementQuery : IRequest<Response<List<DistributorDetailDto>>>;

    public class ListDistributorsForManagementHandler(IGenericRepository<Distributor> distributors)
        : IRequestHandler<ListDistributorsForManagementQuery, Response<List<DistributorDetailDto>>>
    {
        public async Task<Response<List<DistributorDetailDto>>> Handle(ListDistributorsForManagementQuery request, CancellationToken cancellationToken)
        {
            var all = await distributors.GetAllAsync();
            var items = all
                .OrderByDescending(d => d.IsActive)
                .ThenBy(d => d.Name, StringComparer.CurrentCulture)
                .Select(DistributorDetailDto.From)
                .ToList();
            return new Response<List<DistributorDetailDto>>(items);
        }
    }

    public record GetDistributorQuery(int Id) : IRequest<Response<DistributorDetailDto>>;

    public class GetDistributorHandler(IGenericRepository<Distributor> distributors)
        : IRequestHandler<GetDistributorQuery, Response<DistributorDetailDto>>
    {
        public async Task<Response<DistributorDetailDto>> Handle(GetDistributorQuery request, CancellationToken cancellationToken)
        {
            var distributor = await distributors.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException("Distribuidora " + request.Id + " no encontrada.");
            return new Response<DistributorDetailDto>(DistributorDetailDto.From(distributor));
        }
    }

    // ---------------- alta y edición ----------------

    public abstract class DistributorFields
    {
        public string Name { get; set; } = string.Empty;
        public DistributorType Type { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public decimal AvailableThresholdPct { get; set; } = 70m;
        public decimal ConditionedThresholdPct { get; set; } = 30m;
        public decimal DefaultPowerFactor { get; set; } = 0.9m;
    }

    public abstract class DistributorFieldsValidator<T> : AbstractValidator<T> where T : DistributorFields
    {
        protected DistributorFieldsValidator()
        {
            RuleFor(c => c.Name)
                .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no puede exceder los 100 caracteres.");
            RuleFor(c => c.Type).IsInEnum().WithMessage("Tipo de distribuidora inválido.");
            RuleFor(c => c.ContactEmail)
                .EmailAddress().WithMessage("El correo de contacto no es válido.")
                .MaximumLength(150).WithMessage("El correo no puede exceder los 150 caracteres.")
                .When(c => !string.IsNullOrWhiteSpace(c.ContactEmail));
            RuleFor(c => c.ContactPhone).MaximumLength(20).WithMessage("El teléfono no puede exceder los 20 caracteres.");

            // RN-02: Disponible >= umbral alto, Condicionada >= umbral bajo; el alto tiene que quedar por encima.
            RuleFor(c => c.AvailableThresholdPct).InclusiveBetween(1m, 100m)
                .WithMessage("El umbral de disponible debe estar entre 1 y 100 %.");
            RuleFor(c => c.ConditionedThresholdPct).GreaterThan(0m)
                .WithMessage("El umbral de condicionada debe ser mayor que 0 %.");
            RuleFor(c => c.ConditionedThresholdPct).LessThan(c => c.AvailableThresholdPct)
                .WithMessage("El umbral de condicionada debe ser menor que el de disponible.");
            RuleFor(c => c.DefaultPowerFactor).InclusiveBetween(0.5m, 1m)
                .WithMessage("El factor de potencia debe estar entre 0,5 y 1.");
        }
    }

    public class CreateDistributorCommand : DistributorFields, IRequest<Response<DistributorDetailDto>> { }

    public class CreateDistributorValidator : DistributorFieldsValidator<CreateDistributorCommand> { }

    public class CreateDistributorHandler(IGenericRepository<Distributor> distributors, IUnitOfWork unitOfWork)
        : IRequestHandler<CreateDistributorCommand, Response<DistributorDetailDto>>
    {
        public async Task<Response<DistributorDetailDto>> Handle(CreateDistributorCommand command, CancellationToken cancellationToken)
        {
            var name = command.Name.Trim();
            await DistributorNames.EnsureUniqueAsync(distributors, name, exceptId: null);

            var distributor = new Distributor();
            DistributorNames.Apply(distributor, command, name);

            await distributors.AddAsync(distributor);
            await unitOfWork.SaveChangesAsync();

            return new Response<DistributorDetailDto>(DistributorDetailDto.From(distributor), "Distribuidora creada.");
        }
    }

    public class UpdateDistributorCommand : DistributorFields, IRequest<Response<DistributorDetailDto>>
    {
        public int Id { get; set; }
    }

    public class UpdateDistributorValidator : DistributorFieldsValidator<UpdateDistributorCommand> { }

    public class UpdateDistributorHandler(
        IGenericRepository<Distributor> distributors, ITransformerRepository transformers, IUnitOfWork unitOfWork)
        : IRequestHandler<UpdateDistributorCommand, Response<DistributorDetailDto>>
    {
        public async Task<Response<DistributorDetailDto>> Handle(UpdateDistributorCommand command, CancellationToken cancellationToken)
        {
            var distributor = await distributors.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException("Distribuidora " + command.Id + " no encontrada.");

            var name = command.Name.Trim();
            await DistributorNames.EnsureUniqueAsync(distributors, name, exceptId: distributor.Id);

            var thresholdsChanged = distributor.AvailableThresholdPct != command.AvailableThresholdPct
                                    || distributor.ConditionedThresholdPct != command.ConditionedThresholdPct;

            DistributorNames.Apply(distributor, command, name);

            // RN-02: con otros umbrales cambia el estado de cada transformador de la EDE (el mapa se refresca solo).
            if (thresholdsChanged)
            {
                foreach (var transformer in await transformers.GetByDistributorForUpdateAsync(distributor.Id, cancellationToken))
                    transformer.RecalculateStatus(distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);
            }

            await unitOfWork.SaveChangesAsync();

            return new Response<DistributorDetailDto>(DistributorDetailDto.From(distributor), "Distribuidora actualizada.");
        }
    }

    // ---------------- activar y desactivar ----------------

    /// <summary>Desactivar no borra nada: la EDE deja de salir en los catálogos y no admite altas nuevas.</summary>
    public record SetDistributorActiveCommand(int Id, bool IsActive) : IRequest<Response<DistributorDetailDto>>;

    public class SetDistributorActiveHandler(IGenericRepository<Distributor> distributors, IUnitOfWork unitOfWork)
        : IRequestHandler<SetDistributorActiveCommand, Response<DistributorDetailDto>>
    {
        public async Task<Response<DistributorDetailDto>> Handle(SetDistributorActiveCommand command, CancellationToken cancellationToken)
        {
            var distributor = await distributors.GetByIdAsync(command.Id)
                ?? throw new KeyNotFoundException("Distribuidora " + command.Id + " no encontrada.");

            distributor.IsActive = command.IsActive;
            await unitOfWork.SaveChangesAsync();

            return new Response<DistributorDetailDto>(DistributorDetailDto.From(distributor),
                command.IsActive ? "Distribuidora reactivada." : "Distribuidora desactivada.");
        }
    }

    internal static class DistributorNames
    {
        /// <summary>Dos EDE con el mismo nombre confundirían el mapa y los reportes (sin distinguir mayúsculas).</summary>
        public static async Task EnsureUniqueAsync(IGenericRepository<Distributor> distributors, string name, int? exceptId)
        {
            var all = await distributors.GetAllAsync();
            if (all.Any(d => d.Id != exceptId && string.Equals(d.Name.Trim(), name, StringComparison.CurrentCultureIgnoreCase)))
                throw new ApiException("Ya existe una distribuidora llamada " + name + ".");
        }

        public static void Apply(Distributor distributor, DistributorFields fields, string name)
        {
            distributor.Name = name;
            distributor.Type = fields.Type;
            distributor.ContactEmail = string.IsNullOrWhiteSpace(fields.ContactEmail) ? null : fields.ContactEmail.Trim();
            distributor.ContactPhone = string.IsNullOrWhiteSpace(fields.ContactPhone) ? null : fields.ContactPhone.Trim();
            distributor.AvailableThresholdPct = fields.AvailableThresholdPct;
            distributor.ConditionedThresholdPct = fields.ConditionedThresholdPct;
            distributor.DefaultPowerFactor = fields.DefaultPowerFactor;
        }
    }
}
