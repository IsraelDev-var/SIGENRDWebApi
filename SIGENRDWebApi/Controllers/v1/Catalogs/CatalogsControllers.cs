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

    /// <summary>Distribuidoras activas, para filtrar el mapa (B-1). La gestión está en DistributorManagementController.</summary>
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class DistributorsController(IMediator mediator) : BaseApiController
    {
        [ProducesResponseType<Response<List<DistributorDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> List() => Ok(await mediator.Send(new ListDistributorsQuery()));
    }

    /// <summary>
    /// Documentos que pide cada trámite antes de crear la solicitud (B-4): ?requestType=NetMetering&amp;propertyRelation=Tenant.
    /// Público: lo usa la página de requisitos del portal.
    /// </summary>
    [ApiVersion("1.0")]
    [AllowAnonymous]
    public class RequiredDocumentsController(IMediator mediator) : BaseApiController
    {
        [ProducesResponseType<Response<List<DocumentRequirementDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> List([FromQuery] GetDocumentRequirementsQuery query) => Ok(await mediator.Send(query));
    }
}
