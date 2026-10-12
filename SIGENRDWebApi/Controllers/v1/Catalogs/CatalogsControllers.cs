using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Catalogs;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Catalogs
{
    /// <summary>Zonas de servicio, para filtrar el mapa (B-1, RF-03). Público y en caché como el mapa.</summary>
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class ServiceZonesController(IMediator mediator) : BaseApiController
    {
        [ProducesResponseType<Response<List<ServiceZoneDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> List() => Ok(await mediator.Send(new ListServiceZonesQuery()));
    }

    /// <summary>Distribuidoras (en la Fase I solo Edesur), para filtrar el mapa (B-1).</summary>
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class DistributorsController(IMediator mediator) : BaseApiController
    {
        [ProducesResponseType<Response<List<DistributorDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> List() => Ok(await mediator.Send(new ListDistributorsQuery()));
    }
}
