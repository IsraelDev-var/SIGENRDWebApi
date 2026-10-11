using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.SetTransformerMaintenance
{
    public class SetTransformerMaintenanceHandler(IUnitOfWork unitOfWork, ITransformerRepository repository)
        : IRequestHandler<SetTransformerMaintenanceCommand, Response<TransformerDetailDto>>
    {
        public async Task<Response<TransformerDetailDto>> Handle(SetTransformerMaintenanceCommand request, CancellationToken cancellationToken)
        {
            var transformer = await repository.GetByIdAsync(request.Id, asTracking: true, cancellationToken)
                ?? throw new KeyNotFoundException("Transformador " + request.Id + " no encontrado.");

            if (request.InMaintenance)
            {
                transformer.EnterMaintenance();
            }
            else
            {
                var distributor = transformer.Distributor!;
                transformer.LeaveMaintenance(distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);
            }

            await unitOfWork.SaveChangesAsync();

            return new Response<TransformerDetailDto>(TransformerMapper.ToDetail(transformer));
        }
    }
}
