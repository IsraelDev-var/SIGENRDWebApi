using NetTopologySuite.Geometries;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Exceptions;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    /// <summary>
    /// Crea la solicitud como borrador (D-4). No avisa a la distribuidora ni al solicitante y no reserva capacidad:
    /// eso ocurre al enviarla (POST /{id}/submit), cuando ya tiene los documentos.
    /// </summary>
    public class CreateConnectionRequestHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ITransformerRepository transformers,
        ICustomerRepository customers,
        IInstallerCompanyRepository companies)
        : IRequestHandler<CreateConnectionRequestCommand, Response<int>>
    {
        public async Task<Response<int>> Handle(CreateConnectionRequestCommand request, CancellationToken cancellationToken)
        {
            var (customer, installerCompanyId) = await ResolvePartiesAsync(request, cancellationToken);
            var transformer = await ConnectionRequestProjectData.LoadTransformerAsync(transformers, request, cancellationToken);

            var entity = new ConnectionRequest
            {
                Customer = customer.Id == 0 ? customer : null,
                CustomerId = customer.Id,
                InstallerCompanyId = installerCompanyId,
                Status = RequestStatus.Draft,
                RequestedAt = DateTime.UtcNow,
                RequestCode = $"REQ-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            };
            ConnectionRequestProjectData.Apply(entity, request);

            // RN-10: la creación del borrador es la primera entrada del historial.
            entity.StateHistories = new List<StateHistory>
            {
                new() { PreviousState = null, NewState = RequestStatus.Draft.ToString(), Comment = "Borrador creado para el transformador " + transformer.Code }
            };

            await unitOfWork.Repository<ConnectionRequest>().AddAsync(entity);
            await unitOfWork.SaveChangesAsync();

            return new Response<int>(entity.Id, "Borrador creado. Sube los documentos y envía la solicitud.");
        }

        /// <summary>
        /// RN-22 y matriz 3.2: solo el solicitante crea solicitudes. El cliente, a su nombre; la instaladora validada,
        /// a nombre del titular que indica (B-11): el cliente con esa cédula o RNC, o uno nuevo sin cuenta.
        /// </summary>
        private async Task<(Customer Customer, int? InstallerCompanyId)> ResolvePartiesAsync(
            CreateConnectionRequestCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new ForbiddenException();

            if (currentUser.IsInRole(nameof(UserType.Customer)))
            {
                var own = await customers.GetByUserIdAsync(userId, cancellationToken)
                    ?? throw new ApiException("Completa tu perfil de cliente antes de crear una solicitud.");

                if (request.CustomerId.HasValue && request.CustomerId != own.Id)
                    throw new ForbiddenException("La solicitud debe crearse a nombre del cliente autenticado.");
                if (request.InstallerCompanyId.HasValue)
                    throw new ForbiddenException("Un cliente no puede asignar una empresa instaladora.");
                if (request.Holder is not null)
                    throw new ApiException("El cliente solicita a su nombre: no se indica otro titular.");

                return (own, null);
            }

            if (currentUser.IsInRole(nameof(UserType.InstallerCompany)))
            {
                var company = await companies.GetByUserIdAsync(userId, cancellationToken)
                    ?? throw new ApiException("Registra tu empresa antes de crear una solicitud.");

                if (request.InstallerCompanyId.HasValue && request.InstallerCompanyId != company.Id)
                    throw new ForbiddenException("La empresa instaladora no pertenece al usuario autenticado.");
                if (company.ValidationStatus != InstallerValidationStatus.Validated)
                    throw new ForbiddenException("La empresa instaladora aún no ha sido validada (RN-05).");
                if (request.Holder is null)
                    throw new ApiException("Indica los datos del titular del servicio.");

                var holder = await customers.GetByNationalIdAsync(request.Holder.NationalIdOrRnc, cancellationToken);
                if (holder is null)
                {
                    holder = new Customer
                    {
                        ApplicationUserId = null,
                        FullName = request.Holder.FullName.Trim(),
                        NationalIdOrRnc = request.Holder.NationalIdOrRnc.Trim(),
                        IsLegalEntity = request.Holder.IsLegalEntity,
                        Email = request.Holder.Email?.Trim(),
                        Phone = request.Holder.Phone?.Trim(),
                        Address = request.Holder.Address?.Trim()
                    };
                    await customers.AddAsync(holder, cancellationToken);
                }

                return (holder, company.Id);
            }

            throw new ForbiddenException();
        }
    }

    /// <summary>Edita el proyecto de un borrador propio. Fuera de Borrador, los datos ya no cambian.</summary>
    public class UpdateDraftRequestHandler(
        IConnectionRequestRepository requests,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ITransformerRepository transformers)
        : IRequestHandler<UpdateDraftRequestCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(UpdateDraftRequestCommand request, CancellationToken cancellationToken)
        {
            var entity = await requests.GetForUpdateAsync(request.Id, Services.RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + request.Id + " no encontrada.");

            if (entity.Status != RequestStatus.Draft)
                throw new InvalidStateTransitionException(entity.Status, RequestStatus.Draft);

            await ConnectionRequestProjectData.LoadTransformerAsync(transformers, request, cancellationToken);
            ConnectionRequestProjectData.Apply(entity, request);
            await unitOfWork.SaveChangesAsync();

            return new Response<ConnectionRequestResponseDto>(ConnectionRequestMapper.ToResponse(entity), "Borrador actualizado.");
        }
    }

    /// <summary>Copia los datos del proyecto a la entidad: lo mismo al crear el borrador y al editarlo.</summary>
    public static class ConnectionRequestProjectData
    {
        /// <summary>RN-08: un transformador real de la distribuidora indicada. RN-03: que admita solicitudes nuevas.</summary>
        public static async Task<Transformer> LoadTransformerAsync(
            ITransformerRepository transformers, ConnectionRequestData data, CancellationToken cancellationToken)
        {
            var transformer = await transformers.GetByIdAsync(data.TransformerId, asTracking: false, cancellationToken)
                ?? throw new ApiException("El transformador indicado no existe.");

            if (transformer.DistributorId != data.DistributorId)
                throw new ApiException("El transformador no pertenece a la distribuidora indicada.");

            if (!transformer.AcceptsNewRequests)
                throw new ApiException("El transformador " + transformer.Code + " está " + transformer.Status +
                                       " y no admite solicitudes nuevas.");
            return transformer;
        }

        public static void Apply(ConnectionRequest entity, ConnectionRequestData data)
        {
            entity.DistributorId = data.DistributorId;
            entity.TransformerId = data.TransformerId;
            entity.RequestType = data.RequestType;
            entity.RequestedCapacityKva = data.RequestedCapacityKva;
            entity.PropertyRelation = data.PropertyRelation;
            entity.OwnerFullName = data.OwnerFullName?.Trim();
            entity.OwnerNationalId = data.OwnerNationalId?.Trim();
            entity.OwnerPhone = data.OwnerPhone?.Trim();
            entity.EngineerFullName = data.EngineerFullName?.Trim();
            entity.EngineerCodiaNumber = data.EngineerCodiaNumber?.Trim();
            entity.EngineerCompany = data.EngineerCompany?.Trim();
            entity.EngineerPhone = data.EngineerPhone?.Trim();
            entity.EngineerEmail = data.EngineerEmail?.Trim();
            entity.ServiceContractNumber = data.ServiceContractNumber?.Trim();
            entity.MeterNumber = data.MeterNumber?.Trim();
            entity.DeclarationAcceptedAt = DateTime.UtcNow;
            entity.UsageType = data.UsageType;
            entity.TariffType = data.TariffType;
            entity.InterconnectionType = data.InterconnectionType;
            entity.ProjectDescription = data.ProjectDescription;
            entity.ProjectAddress = data.ProjectAddress;
            entity.Coordinates = new Point(data.Longitude, data.Latitude) { SRID = 4326 };

            // Medición neta: datos del sistema y de sus generadores (Anexo A, parte I). Al editar se reemplazan.
            entity.NetMeteringRequest = data.RequestType == RequestType.NetMetering && data.NetMetering is not null
                ? new NetMeteringRequest
                {
                    GenerationSystemType = data.NetMetering.GenerationSystemType,
                    InstalledCapacityKw = data.NetMetering.InstalledCapacityKw,
                    Comments = data.NetMetering.Comments,
                    Generators = data.NetMetering.Generators.Select(g => new Generator
                    {
                        GenerationSystemType = g.GenerationSystemType,
                        GeneratorKind = g.GeneratorKind,
                        Manufacturer = g.Manufacturer.Trim(),
                        InverterModel = g.InverterModel?.Trim(),
                        SerialNumber = g.SerialNumber?.Trim(),
                        Phases = g.Phases,
                        RatedPowerKva = g.RatedPowerKva,
                        ConnectionVoltage = g.ConnectionVoltage,
                        NominalCurrent = g.NominalCurrent,
                        PowerFactorMin = g.PowerFactorMin,
                        PowerFactorMax = g.PowerFactorMax,
                        SwitchingType = g.SwitchingType,
                        HarmonicDistortionPercent = g.HarmonicDistortionPercent,
                        CertificationStandard = g.CertificationStandard,
                        CertifyingUnit = g.CertifyingUnit
                    }).ToList()
                }
                : null;
        }
    }
}
