using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetCriticalZones
{
    /// <summary>Zonas con transformadores críticos o saturados (RF-29).</summary>
    public record GetCriticalZonesQuery : IRequest<Response<List<ZoneRiskDto>>>;
}
