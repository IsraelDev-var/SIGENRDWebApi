using FluentValidation;
using SIGENRD.Core.Application.Mediator;
using Microsoft.Extensions.Logging;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.InstallerCompanies
{
    // Empresas instaladoras: se registran solas y quedan Pendientes hasta que la distribuidora las valida (RN-05, RF-10).

    public class InstallerCompanyDto
    {
        public int Id { get; set; }
        public string TradeName { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string ValidationStatus { get; set; } = string.Empty;

        public static InstallerCompanyDto From(InstallerCompany c) => new()
        {
            Id = c.Id, TradeName = c.TradeName, Rnc = c.Rnc, ContactEmail = c.ContactEmail, ContactPhone = c.ContactPhone,
            Address = c.Address, ValidationStatus = c.ValidationStatus.ToString()
        };
    }

    public class RegisterInstallerCompanyCommand : IRequest<Response<InstallerCompanyDto>>
    {
        public string TradeName { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
    }

    public record GetMyInstallerCompanyQuery : IRequest<Response<InstallerCompanyDto>>;

    public class ListInstallerCompaniesQuery : IRequest<Response<List<InstallerCompanyDto>>>
    {
        public InstallerValidationStatus? Status { get; set; }
    }

    public record ValidateInstallerCompanyCommand(int Id) : IRequest<Response<InstallerCompanyDto>>;

    /// <summary>
    /// Edita los datos de la empresa (B-6, RF-09). El RNC no cambia: es lo que la distribuidora validó (RN-05).
    /// </summary>
    public class UpdateMyInstallerCompanyCommand : IRequest<Response<InstallerCompanyDto>>
    {
        public string TradeName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
    }

    public class UpdateMyInstallerCompanyValidator : AbstractValidator<UpdateMyInstallerCompanyCommand>
    {
        public UpdateMyInstallerCompanyValidator()
        {
            RuleFor(c => c.TradeName).NotEmpty().MaximumLength(150).WithMessage("El nombre comercial es obligatorio (máximo 150 caracteres).");
            RuleFor(c => c.ContactEmail).EmailAddress().MaximumLength(150).When(c => !string.IsNullOrWhiteSpace(c.ContactEmail))
                .WithMessage("El correo de contacto no es válido.");
            RuleFor(c => c.ContactPhone).MaximumLength(20).WithMessage("El teléfono no puede exceder los 20 caracteres.");
            RuleFor(c => c.Address).MaximumLength(250).WithMessage("La dirección no puede exceder los 250 caracteres.");
        }
    }

    public class UpdateMyInstallerCompanyHandler(IInstallerCompanyRepository companies, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        : IRequestHandler<UpdateMyInstallerCompanyCommand, Response<InstallerCompanyDto>>
    {
        public async Task<Response<InstallerCompanyDto>> Handle(UpdateMyInstallerCompanyCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();
            var mine = await companies.GetByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Aún no tienes una empresa instaladora registrada.");

            var company = (await companies.GetForUpdateAsync(mine.Id, cancellationToken))!;
            company.TradeName = request.TradeName.Trim();
            company.ContactEmail = Blank(request.ContactEmail);
            company.ContactPhone = Blank(request.ContactPhone);
            company.Address = Blank(request.Address);
            await unitOfWork.SaveChangesAsync();

            return new Response<InstallerCompanyDto>(InstallerCompanyDto.From(company), "Datos de la empresa actualizados.");
        }

        private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public class RegisterInstallerCompanyValidator : AbstractValidator<RegisterInstallerCompanyCommand>
    {
        public RegisterInstallerCompanyValidator()
        {
            RuleFor(c => c.TradeName).NotEmpty().MaximumLength(150).WithMessage("El nombre comercial es obligatorio (máximo 150 caracteres).");
            RuleFor(c => c.Rnc).NotEmpty().MaximumLength(20).WithMessage("El RNC es obligatorio (máximo 20 caracteres).");
            RuleFor(c => c.ContactEmail).EmailAddress().MaximumLength(150).When(c => !string.IsNullOrWhiteSpace(c.ContactEmail))
                .WithMessage("El correo de contacto no es válido.");
            RuleFor(c => c.ContactPhone).MaximumLength(20);
        }
    }

    public class ListInstallerCompaniesValidator : AbstractValidator<ListInstallerCompaniesQuery>
    {
        public ListInstallerCompaniesValidator()
        {
            RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue).WithMessage("Estado de validación inválido.");
        }
    }

    public class RegisterInstallerCompanyHandler(
        IInstallerCompanyRepository companies, IUnitOfWork unitOfWork, ICurrentUserService currentUser, StaffNotifier staffNotifier)
        : IRequestHandler<RegisterInstallerCompanyCommand, Response<InstallerCompanyDto>>
    {
        public async Task<Response<InstallerCompanyDto>> Handle(RegisterInstallerCompanyCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            if (await companies.GetByUserIdAsync(userId, cancellationToken) is not null)
                throw new ApiException("Tu usuario ya tiene una empresa instaladora registrada.");

            var rnc = request.Rnc.Trim();
            if (await companies.RncExistsAsync(rnc, cancellationToken))
                throw new ApiException("Ya existe una empresa instaladora con ese RNC.");

            var company = new InstallerCompany
            {
                TradeName = request.TradeName.Trim(),
                Rnc = rnc,
                ContactEmail = request.ContactEmail?.Trim(),
                ContactPhone = request.ContactPhone?.Trim(),
                Address = request.Address?.Trim(),
                ApplicationUserId = userId,
                ValidationStatus = InstallerValidationStatus.Pending
            };

            await companies.AddAsync(company, cancellationToken);

            // La distribuidora debe saber que hay una empresa esperando validación.
            await staffNotifier.NotifyAsync(
                "Empresa instaladora por validar",
                "La empresa " + company.TradeName + " (RNC " + company.Rnc + ") solicitó su validación.",
                NotificationType.InstallerPendingValidation,
                cancellationToken);

            await unitOfWork.SaveChangesAsync();

            return new Response<InstallerCompanyDto>(InstallerCompanyDto.From(company), "Empresa registrada. Queda pendiente de validación.");
        }
    }

    public class GetMyInstallerCompanyHandler(IInstallerCompanyRepository companies, ICurrentUserService currentUser)
        : IRequestHandler<GetMyInstallerCompanyQuery, Response<InstallerCompanyDto>>
    {
        public async Task<Response<InstallerCompanyDto>> Handle(GetMyInstallerCompanyQuery request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            var company = await companies.GetByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Aún no tienes una empresa instaladora registrada.");

            return new Response<InstallerCompanyDto>(InstallerCompanyDto.From(company));
        }
    }

    public class ListInstallerCompaniesHandler(IInstallerCompanyRepository companies)
        : IRequestHandler<ListInstallerCompaniesQuery, Response<List<InstallerCompanyDto>>>
    {
        public async Task<Response<List<InstallerCompanyDto>>> Handle(ListInstallerCompaniesQuery request, CancellationToken cancellationToken)
        {
            var list = await companies.ListAsync(request.Status, cancellationToken);
            return new Response<List<InstallerCompanyDto>>(list.Select(InstallerCompanyDto.From).ToList());
        }
    }

    public class ValidateInstallerCompanyHandler(
        IInstallerCompanyRepository companies, IUnitOfWork unitOfWork, IEmailService emailService,
        ILogger<ValidateInstallerCompanyHandler> logger)
        : IRequestHandler<ValidateInstallerCompanyCommand, Response<InstallerCompanyDto>>
    {
        public async Task<Response<InstallerCompanyDto>> Handle(ValidateInstallerCompanyCommand request, CancellationToken cancellationToken)
        {
            var company = await companies.GetForUpdateAsync(request.Id, cancellationToken)
                ?? throw new KeyNotFoundException("Empresa instaladora " + request.Id + " no encontrada.");

            if (company.ValidationStatus == InstallerValidationStatus.Validated)
                throw new ApiException("La empresa ya está validada.");

            company.ValidationStatus = InstallerValidationStatus.Validated;

            if (!string.IsNullOrWhiteSpace(company.ApplicationUserId))
            {
                await unitOfWork.Repository<Notification>().AddAsync(new Notification
                {
                    UserId = company.ApplicationUserId,
                    Title = "Empresa validada",
                    Message = "La distribuidora validó a " + company.TradeName + ". Ya puedes crear solicitudes.",
                    NotificationType = NotificationType.InstallerValidated
                });
            }

            await unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(company.ContactEmail))
            {
                try
                {
                    await emailService.SendAsync(new EmailRequest
                    {
                        To = company.ContactEmail,
                        Subject = "Empresa instaladora validada",
                        Body = "<p>La distribuidora validó a " + System.Net.WebUtility.HtmlEncode(company.TradeName) +
                               ". Ya puede crear solicitudes en la plataforma.</p>"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "No se pudo avisar por correo la validación de {Company}", company.TradeName);
                }
            }

            return new Response<InstallerCompanyDto>(InstallerCompanyDto.From(company), "Empresa validada.");
        }
    }
}
