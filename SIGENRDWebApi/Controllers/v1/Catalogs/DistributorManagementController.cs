using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Distributors;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Catalogs
{
    /// <summary>Gestión de las EDE: alta, edición (contacto, umbrales, factor de potencia) y desactivación. Solo el administrador.</summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Distributors/manage")]
    [Authorize(Roles = Roles.Admin)]
    public class DistributorManagementController(IMediator mediator) : BaseApiController
    {
        /// <summary>Todas las EDE, también las desactivadas.</summary>
        [ProducesResponseType<Response<List<DistributorDetailDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> List() => Ok(await mediator.Send(new ListDistributorsForManagementQuery()));

        [ProducesResponseType<Response<DistributorDetailDto>>(StatusCodes.Status200OK)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id) => Ok(await mediator.Send(new GetDistributorQuery(id)));

        [ProducesResponseType<Response<DistributorDetailDto>>(StatusCodes.Status200OK)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDistributorCommand command) => Ok(await mediator.Send(command));

        /// <summary>Si cambian los umbrales, se reclasifican los transformadores de la EDE (RN-02).</summary>
        [ProducesResponseType<Response<DistributorDetailDto>>(StatusCodes.Status200OK)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDistributorCommand command)
        {
            command.Id = id;
            return Ok(await mediator.Send(command));
        }

        [ProducesResponseType<Response<DistributorDetailDto>>(StatusCodes.Status200OK)]
        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id) => Ok(await mediator.Send(new SetDistributorActiveCommand(id, false)));

        [ProducesResponseType<Response<DistributorDetailDto>>(StatusCodes.Status200OK)]
        [HttpPost("{id:int}/reactivate")]
        public async Task<IActionResult> Reactivate(int id) => Ok(await mediator.Send(new SetDistributorActiveCommand(id, true)));
    }
}
