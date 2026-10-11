using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Simulations;
using SIGENRD.Core.Application.Mediator;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Simulations
{
    /// <summary>
    /// Simulación de medición neta (RF-18 a RF-26): informativa, sin valor regulatorio. Cada usuario ve solo las suyas.
    /// </summary>
    [ApiVersion("1.0")]
    [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}")]
    public class SimulationsController(IMediator mediator) : BaseApiController
    {
        /// <summary>Calcula producción, ahorro, ROI y retorno; clasifica y recomienda; queda en el historial.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSimulationCommand command)
            => Ok(await mediator.Send(command));

        /// <summary>Historial propio, más recientes primero (RF-26).</summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ListMySimulationsQuery query)
            => Ok(await mediator.Send(query));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
            => Ok(await mediator.Send(new GetSimulationQuery(id)));

        /// <summary>Compara de 2 a 5 simulaciones propias (RF-22): ?ids=1&amp;ids=2.</summary>
        [HttpGet("compare")]
        public async Task<IActionResult> Compare([FromQuery] CompareSimulationsQuery query)
            => Ok(await mediator.Send(query));

        /// <summary>Resumen en PDF (CA-10).</summary>
        [HttpGet("{id:int}/report")]
        public async Task<IActionResult> Report(int id, CancellationToken cancellationToken)
        {
            var file = await mediator.Send(new GetSimulationReportQuery(id), cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}
