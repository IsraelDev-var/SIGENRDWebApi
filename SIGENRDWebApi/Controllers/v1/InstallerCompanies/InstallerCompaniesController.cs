using SIGENRD.Core.Application.Wrappers;
using Asp.Versioning;
using SIGENRD.Core.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.InstallerCompanies;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.InstallerCompanies
{
    [ApiVersion("1.0")]
    public class InstallerCompaniesController(IMediator mediator) : BaseApiController
    {
        /// <summary>Registra la empresa del usuario. Queda Pendiente hasta que la distribuidora la valide (RN-05).</summary>
        [ProducesResponseType<Response<InstallerCompanyDto>>(StatusCodes.Status200OK)]
        [HttpPost("me")]
        [Authorize(Roles = Roles.InstallerCompany)]
        public async Task<IActionResult> RegisterMine([FromBody] RegisterInstallerCompanyCommand command)
            => Ok(await mediator.Send(command));

        [ProducesResponseType<Response<InstallerCompanyDto>>(StatusCodes.Status200OK)]
        [HttpGet("me")]
        [Authorize(Roles = Roles.InstallerCompany)]
        public async Task<IActionResult> GetMine()
            => Ok(await mediator.Send(new GetMyInstallerCompanyQuery()));

        /// <summary>Empresas instaladoras, por estado de validación (RF-10).</summary>
        [ProducesResponseType<Response<List<InstallerCompanyDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> List([FromQuery] ListInstallerCompaniesQuery query)
            => Ok(await mediator.Send(query));

        /// <summary>Valida la empresa para que pueda crear solicitudes (RN-05, RF-10).</summary>
        [ProducesResponseType<Response<InstallerCompanyDto>>(StatusCodes.Status200OK)]
        [HttpPost("{id:int}/validate")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> Validate(int id)
            => Ok(await mediator.Send(new ValidateInstallerCompanyCommand(id)));
    }
}
