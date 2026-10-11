using FluentValidation;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Customers
{
    // El perfil de cliente liga al usuario de Identity con sus solicitudes (RN-22). Sin perfil no se puede solicitar.

    public class CustomerProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string NationalIdOrRnc { get; set; } = string.Empty;
        public bool IsLegalEntity { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        public static CustomerProfileDto From(Customer c) => new()
        {
            Id = c.Id, FullName = c.FullName, NationalIdOrRnc = c.NationalIdOrRnc, IsLegalEntity = c.IsLegalEntity,
            Email = c.Email, Phone = c.Phone, Address = c.Address
        };
    }

    public class CreateMyCustomerProfileCommand : IRequest<Response<CustomerProfileDto>>
    {
        public string FullName { get; set; } = string.Empty;

        /// <summary>Cédula, pasaporte o RNC.</summary>
        public string NationalIdOrRnc { get; set; } = string.Empty;

        /// <summary>Persona jurídica: cambia los documentos que se exigen.</summary>
        public bool IsLegalEntity { get; set; }

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public record GetMyCustomerProfileQuery : IRequest<Response<CustomerProfileDto>>;

    public class CreateMyCustomerProfileValidator : AbstractValidator<CreateMyCustomerProfileCommand>
    {
        public CreateMyCustomerProfileValidator()
        {
            RuleFor(c => c.FullName).NotEmpty().MaximumLength(150).WithMessage("El nombre es obligatorio (máximo 150 caracteres).");
            RuleFor(c => c.NationalIdOrRnc).NotEmpty().MaximumLength(20).WithMessage("La cédula o el RNC es obligatorio (máximo 20 caracteres).");
            RuleFor(c => c.Email).EmailAddress().MaximumLength(150).When(c => !string.IsNullOrWhiteSpace(c.Email))
                .WithMessage("El correo no es válido.");
            RuleFor(c => c.Phone).MaximumLength(30);
            RuleFor(c => c.Address).MaximumLength(250);
        }
    }

    public class CreateMyCustomerProfileHandler(
        ICustomerRepository customers, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        : IRequestHandler<CreateMyCustomerProfileCommand, Response<CustomerProfileDto>>
    {
        public async Task<Response<CustomerProfileDto>> Handle(CreateMyCustomerProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            if (await customers.GetByUserIdAsync(userId, cancellationToken) is not null)
                throw new ApiException("Ya tienes un perfil de cliente.");

            var nationalId = request.NationalIdOrRnc.Trim();
            if (await customers.NationalIdExistsAsync(nationalId, cancellationToken))
                throw new ApiException("Ya existe un cliente con esa cédula o RNC.");

            var customer = new Customer
            {
                ApplicationUserId = userId,
                FullName = request.FullName.Trim(),
                NationalIdOrRnc = nationalId,
                IsLegalEntity = request.IsLegalEntity,
                Email = request.Email?.Trim(),
                Phone = request.Phone?.Trim(),
                Address = request.Address?.Trim()
            };

            await customers.AddAsync(customer, cancellationToken);
            await unitOfWork.SaveChangesAsync();

            return new Response<CustomerProfileDto>(CustomerProfileDto.From(customer), "Perfil creado.");
        }
    }

    public class GetMyCustomerProfileHandler(ICustomerRepository customers, ICurrentUserService currentUser)
        : IRequestHandler<GetMyCustomerProfileQuery, Response<CustomerProfileDto>>
    {
        public async Task<Response<CustomerProfileDto>> Handle(GetMyCustomerProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            var customer = await customers.GetByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Aún no tienes un perfil de cliente.");

            return new Response<CustomerProfileDto>(CustomerProfileDto.From(customer));
        }
    }
}
