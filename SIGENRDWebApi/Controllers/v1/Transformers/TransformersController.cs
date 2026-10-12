using Asp.Versioning;
using SIGENRD.Core.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.Transformers.Commands.CreateTransformer;
using SIGENRD.Core.Application.Features.Transformers.Commands.ImportTransformers;
using SIGENRD.Core.Application.Features.Transformers.Commands.SetTransformerMaintenance;
using SIGENRD.Core.Application.Features.Transformers.Commands.UpdateTransformerCapacity;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetCriticalZones;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformerById;
using SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds;
using SIGENRD.Core.Application.Features.Transformers.Queries.SearchTransformers;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Transformers
{
    [ApiVersion("1.0")]
    public class TransformersController(IMediator mediator) : BaseApiController
    {
        // Máximo del archivo CSV de importación (10.000 filas caben de sobra)
        private const long MaxCsvBytes = 5 * 1024 * 1024;

        // ---------- Consulta pública (RF-01 a RF-05): el mapa es público ----------

        /// <summary>Transformadores dentro del área visible del mapa, con filtros.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> GetInBounds([FromQuery] GetTransformersInBoundsQuery query)
            => Ok(await mediator.Send(query));

        /// <summary>Busca por código o por cercanía a unas coordenadas.</summary>
        [HttpGet("search")]
        [AllowAnonymous]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> Search([FromQuery] SearchTransformersQuery query)
            => Ok(await mediator.Send(query));

        /// <summary>Detalle técnico de un transformador.</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ResponseCache(CacheProfileName = CacheProfiles.Map)]
        public async Task<IActionResult> GetById(int id)
            => Ok(await mediator.Send(new GetTransformerByIdQuery(id)));

        // ---------- Gestión (distribuidora y administrador) ----------

        /// <summary>Zonas con transformadores críticos o saturados (RF-29).</summary>
        [HttpGet("critical-zones")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> GetCriticalZones()
            => Ok(await mediator.Send(new GetCriticalZonesQuery()));

        [HttpPost]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> Create([FromBody] CreateTransformerCommand command)
            => Ok(await mediator.Send(command));

        /// <summary>Informa la capacidad vigente; el estado se recalcula.</summary>
        [HttpPut("{id:int}/capacity")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> UpdateCapacity(int id, [FromBody] UpdateTransformerCapacityCommand command)
        {
            command.Id = id;
            return Ok(await mediator.Send(command));
        }

        [HttpPut("{id:int}/maintenance")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> SetMaintenance(int id, [FromBody] SetTransformerMaintenanceCommand command)
        {
            command.Id = id;
            return Ok(await mediator.Send(command));
        }

        /// <summary>Carga masiva desde la plantilla CSV. Todo o nada.</summary>
        [HttpPost("import")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        [RequestSizeLimit(MaxCsvBytes)]
        public async Task<IActionResult> Import([FromForm] int distributorId, IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Adjunta un archivo CSV.");

            if (file.Length > MaxCsvBytes)
                return BadRequest("El archivo supera el máximo de 5 MB.");

            if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("El archivo debe tener extensión .csv.");

            await using var stream = file.OpenReadStream();

            return Ok(await mediator.Send(new ImportTransformersCommand
            {
                DistributorId = distributorId,
                Content = stream
            }));
        }
    }
}
