using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds
{
    public class GetTransformersInBoundsHandler(ITransformerRepository repository)
        : IRequestHandler<GetTransformersInBoundsQuery, Response<TransformerMapResultDto>>
    {
        public async Task<Response<TransformerMapResultDto>> Handle(GetTransformersInBoundsQuery request, CancellationToken cancellationToken)
        {
            var filter = new TransformerFilter(request.DistributorId, request.ServiceZoneId, request.Status, request.MinAvailabilityPct);

            // Se pide uno más del límite para saber si el resultado quedó recortado.
            var found = await repository.GetInBoundsAsync(
                request.MinLatitude, request.MinLongitude, request.MaxLatitude, request.MaxLongitude,
                filter, request.Limit + 1, cancellationToken);

            var result = new TransformerMapResultDto
            {
                Truncated = found.Count > request.Limit,
                Items = found.Take(request.Limit).Select(TransformerMapper.ToMapPoint).ToList()
            };

            return new Response<TransformerMapResultDto>(result);
        }
    }
}
