using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.SearchTransformers
{
    public class SearchTransformersHandler(ITransformerRepository repository)
        : IRequestHandler<SearchTransformersQuery, Response<List<TransformerMapPointDto>>>
    {
        public async Task<Response<List<TransformerMapPointDto>>> Handle(SearchTransformersQuery request, CancellationToken cancellationToken)
        {
            var filter = new TransformerFilter(request.DistributorId, request.ServiceZoneId, request.Status, request.MinAvailabilityPct);

            IReadOnlyList<Transformer> found;

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                found = await repository.SearchByCodeAsync(request.Code.Trim(), request.Limit, cancellationToken);
            }
            else
            {
                found = await repository.GetNearAsync(
                    request.Latitude!.Value, request.Longitude!.Value, request.RadiusMeters,
                    filter, request.Limit, cancellationToken);
            }

            return new Response<List<TransformerMapPointDto>>(found.Select(TransformerMapper.ToMapPoint).ToList());
        }
    }
}
