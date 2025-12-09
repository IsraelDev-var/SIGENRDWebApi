using MediatR;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using NetTopologySuite.Geometries;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    // (La lógica de guardado)
    public class CreateConnectionRequestHandler : IRequestHandler<CreateConnectionRequestCommand, Response<int>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateConnectionRequestHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<int>> Handle(CreateConnectionRequestCommand request, CancellationToken cancellationToken)
        {
            // Mapeo manual para control total (o podrías usar AutoMapper si prefieres)
            var entity = new ConnectionRequest
            {
                CustomerId = request.CustomerId,
                InstallerCompanyId = request.InstallerCompanyId,
                DistributorId = request.DistributorId,
                TransformerId = (int)request.TransformerId,
                UsageType = request.UsageType,
                TariffType = request.TariffType,
                InterconnectionType = request.InterconnectionType,
                ProjectDescription = request.ProjectDescription,
                ProjectAddress = request.ProjectAddress,

                // Lógica de negocio
                Status = RequestStatus.UnderReview,
                RequestedAt = DateTime.UtcNow,
                RequestCode = $"REQ-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",

                // Coordenadas
                Coordinates = new Point(request.Longitude, request.Latitude) { SRID = 4326 }
            };

            await _unitOfWork.Repository<ConnectionRequest>().AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return new Response<int>(entity.Id, "Solicitud creada exitosamente.");
        }
    }
}
