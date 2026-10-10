using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.Workflow;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetRequestHistory;
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

        // ---------- Historial ----------

        /// <summary>Cambios de estado de la solicitud, del más antiguo al más reciente (RN-10).</summary>
        [HttpGet("{id:int}/history")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> History(int id)
            => Ok(await _mediator.Send(new GetRequestHistoryQuery(id)));

        // ---------- Flujo de estados: personal de la distribuidora ----------

        /// <summary>Recibida a En revisión: valida documentos y reserva la capacidad.</summary>
        [HttpPost("{id:int}/start-review")]
        [Authorize(Roles = $"{Roles.Engineer}, {Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> StartReview(int id)
            => Ok(await _mediator.Send(new StartReviewCommand { Id = id }));

        /// <summary>En revisión a Observaciones. Requiere comentario.</summary>
        [HttpPost("{id:int}/observe")]
        [Authorize(Roles = $"{Roles.Engineer}, {Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> Observe(int id, [FromBody] ObserveRequestCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        /// <summary>En revisión a Aprobada. Verifica documentos y capacidad (RN-09, RN-15).</summary>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> Approve(int id)
            => Ok(await _mediator.Send(new ApproveRequestCommand { Id = id }));

        /// <summary>En revisión a Rechazada. Requiere comentario.</summary>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = $"{Roles.Distributor}, {Roles.Admin}")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectRequestCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        // ---------- Flujo de estados: solicitante ----------

        /// <summary>Observaciones a En corrección.</summary>
        [HttpPost("{id:int}/start-correction")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}")]
        public async Task<IActionResult> StartCorrection(int id)
            => Ok(await _mediator.Send(new StartCorrectionCommand { Id = id }));

        /// <summary>En corrección a En revisión.</summary>
        [HttpPost("{id:int}/resubmit")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}")]
        public async Task<IActionResult> Resubmit(int id)
            => Ok(await _mediator.Send(new ResubmitRequestCommand { Id = id }));

        /// <summary>Cancela la solicitud mientras sea Recibida, Observaciones o En corrección (RN-23).</summary>
        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequestCommand? command)
        {
            command ??= new CancelRequestCommand();
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }
    }
}
