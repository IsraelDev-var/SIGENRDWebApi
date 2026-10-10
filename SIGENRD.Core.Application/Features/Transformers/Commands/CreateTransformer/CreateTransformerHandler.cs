using MediatR;
using NetTopologySuite.Geometries;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.CreateTransformer
{
    public class CreateTransformerHandler(IUnitOfWork unitOfWork, ITransformerRepository repository)
        : IRequestHandler<CreateTransformerCommand, Response<int>>
    {
        public async Task<Response<int>> Handle(CreateTransformerCommand request, CancellationToken cancellationToken)
        {
            var code = request.Code.Trim();

            var distributor = await unitOfWork.Repository<Distributor>().GetByIdAsync(request.DistributorId)
                ?? throw new ApiException("La distribuidora indicada no existe.");

            _ = await unitOfWork.Repository<ServiceZone>().GetByIdAsync(request.ServiceZoneId)
                ?? throw new ApiException("La zona indicada no existe.");

            var existing = await repository.GetExistingCodesAsync(new[] { code }, cancellationToken);
            if (existing.Contains(code))
                throw new ApiException("Ya existe un transformador con el código " + code + ".");

            var transformer = new Transformer
            {
                Code = code,
                DistributorId = request.DistributorId,
                ServiceZoneId = request.ServiceZoneId,
                Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 }
            };

            // Calcula el estado inicial con los umbrales de la distribuidora (RN-02).
            transformer.UpdateCapacity(
                request.TotalCapacityKva, request.AvailableCapacityKva,
                distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);

            await repository.AddAsync(transformer, cancellationToken);
            await unitOfWork.SaveChangesAsync();

            return new Response<int>(transformer.Id, "Transformador creado correctamente.");
        }
    }
}
