using MediatR;
using NetTopologySuite.Geometries;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    // (La lógica de guardado)
    public class CreateConnectionRequestHandler : IRequestHandler<CreateConnectionRequestCommand, Response<int>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateConnectionRequestHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Response<int>> Handle(CreateConnectionRequestCommand request, CancellationToken cancellationToken)
        {
            await EnsureCallerOwnsTheRequestParties(request);

            var entity = new ConnectionRequest
            {
                CustomerId = request.CustomerId,
                InstallerCompanyId = request.InstallerCompanyId,
                DistributorId = request.DistributorId,
                TransformerId = request.TransformerId,
                RequestType = request.RequestType,
                RequestedCapacityKva = request.RequestedCapacityKva,
                UsageType = request.UsageType,
                TariffType = request.TariffType,
                InterconnectionType = request.InterconnectionType,
                ProjectDescription = request.ProjectDescription,
                ProjectAddress = request.ProjectAddress,

                // Lógica de negocio
                Status = RequestStatus.Received,
                RequestedAt = DateTime.UtcNow,
                RequestCode = $"REQ-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",

                // Coordenadas
                Coordinates = new Point(request.Longitude, request.Latitude) { SRID = 4326 }
            };

            await _unitOfWork.Repository<ConnectionRequest>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return new Response<int>(entity.Id, "Solicitud creada exitosamente.");
        }

        /// <summary>
        /// RN-22: un cliente solo crea solicitudes a su nombre y una instaladora solo con su propia empresa.
        /// El administrador puede crear en nombre de cualquiera.
        /// </summary>
        private async Task EnsureCallerOwnsTheRequestParties(CreateConnectionRequestCommand request)
        {
            if (_currentUser.IsInRole(nameof(UserType.Admin)))
                return;

            var userId = _currentUser.UserId ?? throw new ForbiddenException();

            if (_currentUser.IsInRole(nameof(UserType.Customer)))
            {
                var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(request.CustomerId);

                if (customer is null || customer.ApplicationUserId != userId)
                    throw new ForbiddenException("La solicitud debe crearse a nombre del cliente autenticado.");

                if (request.InstallerCompanyId.HasValue)
                    throw new ForbiddenException("Un cliente no puede asignar una empresa instaladora.");

                return;
            }

            if (_currentUser.IsInRole(nameof(UserType.InstallerCompany)))
            {
                if (!request.InstallerCompanyId.HasValue)
                    throw new ForbiddenException("La solicitud debe indicar la empresa instaladora del usuario.");

                var company = await _unitOfWork.Repository<InstallerCompany>().GetByIdAsync(request.InstallerCompanyId.Value);

                if (company is null || company.ApplicationUserId != userId)
                    throw new ForbiddenException("La empresa instaladora no pertenece al usuario autenticado.");

                if (company.ValidationStatus != InstallerValidationStatus.Validated)
                    throw new ForbiddenException("La empresa instaladora aún no ha sido validada (RN-05).");

                return;
            }

            throw new ForbiddenException();
        }
    }
}
