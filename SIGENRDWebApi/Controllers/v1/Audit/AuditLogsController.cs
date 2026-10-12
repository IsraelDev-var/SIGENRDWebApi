using SIGENRD.Core.Application.Wrappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Audit;
using SIGENRD.Core.Application.Mediator;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Audit
{
    /// <summary>Consulta del registro de auditoría (RN-20). Solo administradores.</summary>
    [ApiVersion("1.0")]
    [Authorize(Roles = Roles.Admin)]
    public class AuditLogsController(IMediator mediator) : BaseApiController
    {
        /// <summary>Eventos más recientes primero, con filtros por entidad, usuario, acción y fechas.</summary>
        [ProducesResponseType<Response<PagedDto<AuditLogDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] SearchAuditLogsQuery query)
            => Ok(await mediator.Send(query));
    }
}
