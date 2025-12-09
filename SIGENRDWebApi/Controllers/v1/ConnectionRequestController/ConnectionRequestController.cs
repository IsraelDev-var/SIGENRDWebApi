using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetAllConnectionRequests;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetConnectionRequestById;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.ConnectionRequestController
{
    [ApiVersion("1.0")]
    // bloque de código del controlador
   
    public class ConnectionRequestController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        // 🟢 POST: Crear Solicitud
        [HttpPost]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}")]
        public async Task<IActionResult> Create([FromBody] CreateConnectionRequestCommand command)
        {
            // Enviamos el comando al Handler correspondiente
            return Ok(await _mediator.Send(command));
        }

        // 🟢 GET: Obtener por ID
        [HttpGet("{id}")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> Get(int id)
        {
            // Enviamos el Query al Handler correspondiente
            return Ok(await _mediator.Send(new GetConnectionRequestByIdQuery(id)));
        }

        // 🟢 GET: Obtener Todos
        [HttpGet]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> GetAll()
        {
            // Enviamos el Query al Handler correspondiente
            return Ok(await _mediator.Send(new GetAllConnectionRequestsQuery()));
        }
    }
}
