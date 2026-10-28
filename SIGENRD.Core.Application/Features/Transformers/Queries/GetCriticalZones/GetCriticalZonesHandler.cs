using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetCriticalZones
{
    public class GetCriticalZonesHandler(ITransformerRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<GetCriticalZonesQuery, Response<List<ZoneRiskDto>>>
    {
        public async Task<Response<List<ZoneRiskDto>>> Handle(GetCriticalZonesQuery request, CancellationToken cancellationToken)
        {
            var zones = await repository.GetZoneRiskAsync(DistributorScope.Filter(currentUser), cancellationToken);

            var dtos = zones.Select(z => new ZoneRiskDto
            {
                ServiceZoneId = z.ServiceZoneId,
                ZoneName = z.ZoneName,
                TotalTransformers = z.Total,
                Critical = z.Critical,
                Saturated = z.Saturated
            }).ToList();

            return new Response<List<ZoneRiskDto>>(dtos);
        }
    }
}
