using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.UpdateTransformerCapacity
{
    public class UpdateTransformerCapacityHandler(IUnitOfWork unitOfWork, ITransformerRepository repository)
        : IRequestHandler<UpdateTransformerCapacityCommand, Response<TransformerDetailDto>>
    {
        public async Task<Response<TransformerDetailDto>> Handle(UpdateTransformerCapacityCommand request, CancellationToken cancellationToken)
        {
            var transformer = await repository.GetByIdAsync(request.Id, asTracking: true, cancellationToken)
                ?? throw new KeyNotFoundException("Transformador " + request.Id + " no encontrado.");

            var distributor = transformer.Distributor!;

            transformer.UpdateCapacity(
                request.TotalCapacityKva, request.AvailableCapacityKva,
                distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);

            await unitOfWork.SaveChangesAsync();

            return new Response<TransformerDetailDto>(TransformerMapper.ToDetail(transformer), "Capacidad actualizada.");
        }
    }
}
