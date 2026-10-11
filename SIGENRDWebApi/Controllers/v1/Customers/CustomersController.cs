using Asp.Versioning;
using SIGENRD.Core.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Customers;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Customers
{
    [ApiVersion("1.0")]
    [Authorize(Roles = Roles.Customer)]
    public class CustomersController(IMediator mediator) : BaseApiController
    {
        /// <summary>Crea el perfil del cliente autenticado. Es necesario para crear solicitudes.</summary>
        [HttpPost("me")]
        public async Task<IActionResult> CreateMine([FromBody] CreateMyCustomerProfileCommand command)
            => Ok(await mediator.Send(command));

        /// <summary>Perfil del cliente autenticado.</summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMine()
            => Ok(await mediator.Send(new GetMyCustomerProfileQuery()));
    }
}
