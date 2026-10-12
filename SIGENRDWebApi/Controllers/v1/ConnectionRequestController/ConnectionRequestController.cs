using Asp.Versioning;
using SIGENRD.Core.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest;
using SIGENRD.Core.Application.Features.ConnectionRequests.Commands.Workflow;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetRequestHistory;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetRequiredDocuments;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.SearchAndTrack;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetConnectionRequestById;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Features.Documents;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Domain.Enums;
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
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}")] // matriz 3.2: solo el solicitante
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

        /// <summary>Listado paginado (tamaño máximo de página acotado), con los mismos filtros que /search.</summary>
        [HttpGet]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> GetAll([FromQuery] SearchConnectionRequestsQuery query)
            => Ok(await _mediator.Send(query));

        // ---------- Búsqueda y seguimiento ----------

        /// <summary>Lista paginada con filtros, dentro de lo que el rol puede ver (RN-22).</summary>
        [HttpGet("search")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> Search([FromQuery] SearchConnectionRequestsQuery query)
            => Ok(await _mediator.Send(query));

        /// <summary>Seguimiento público por número de expediente: solo estado y fechas (RF-14).</summary>
        [HttpGet("track/{code}")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PublicTracking)]
        public async Task<IActionResult> Track(string code)
            => Ok(await _mediator.Send(new TrackRequestQuery(code)));

        // ---------- Historial ----------

        /// <summary>Cambios de estado de la solicitud, del más antiguo al más reciente (RN-10).</summary>
        [HttpGet("{id:int}/history")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> History(int id)
            => Ok(await _mediator.Send(new GetRequestHistoryQuery(id)));

        // ---------- Documentos (RF-16, RF-34 a RF-37) ----------

        public class UploadDocumentForm
        {
            public IFormFile? File { get; set; }
            public DocumentType DocumentType { get; set; }
        }

        // Un poco más que el máximo de 10 MB (RN-18) para el resto del formulario multipart.
        private const long UploadLimitBytes = DocumentFileInspector.MaxSizeBytes + 512 * 1024;

        /// <summary>Sube un documento PDF, JPG o PNG de hasta 10 MB. Solo con la solicitud Recibida o En corrección.</summary>
        [HttpPost("{id:int}/documents")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}")] // matriz 3.2: documentos propios
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(UploadLimitBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadLimitBytes)]
        public async Task<IActionResult> UploadDocument(int id, [FromForm] UploadDocumentForm form, CancellationToken cancellationToken)
        {
            if (form.File is null || form.File.Length == 0)
                throw new ApiException("Adjunte el archivo en el campo 'file'.");
            if (form.File.Length > DocumentFileInspector.MaxSizeBytes)
                throw new ApiException("El archivo supera el máximo de 10 MB.");

            using var buffer = new MemoryStream((int)form.File.Length);
            await form.File.CopyToAsync(buffer, cancellationToken);

            return Ok(await _mediator.Send(new UploadDocumentCommand
            {
                RequestId = id,
                DocumentType = form.DocumentType,
                FileName = form.File.FileName,
                Content = buffer.ToArray()
            }, cancellationToken));
        }

        /// <summary>Documentos vigentes del expediente, incluido el certificado si está aprobada.</summary>
        [HttpGet("{id:int}/documents")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> ListDocuments(int id)
            => Ok(await _mediator.Send(new ListDocumentsQuery(id)));

        /// <summary>Descarga un documento. Una solicitud ajena responde 404 (RN-22).</summary>
        [HttpGet("{id:int}/documents/{documentId:int}/content")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> DownloadDocument(int id, int documentId, CancellationToken cancellationToken)
        {
            var file = await _mediator.Send(new DownloadDocumentQuery(id, documentId), cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>Retira un documento (baja lógica). No se puede tras la aprobación (RN-19) ni durante la revisión.</summary>
        [HttpDelete("{id:int}/documents/{documentId:int}")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}")]
        public async Task<IActionResult> DeleteDocument(int id, int documentId)
            => Ok(await _mediator.Send(new DeleteDocumentCommand(id, documentId)));

        /// <summary>Certificado de aprobación en PDF (RN-14, RF-17).</summary>
        [HttpGet("{id:int}/certificate")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> DownloadCertificate(int id, CancellationToken cancellationToken)
        {
            var file = await _mediator.Send(new DownloadCertificateQuery(id), cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>Documentos que exige la solicitud y cuáles ya están presentes (RN-09, RN-11).</summary>
        [HttpGet("{id:int}/required-documents")]
        [Authorize(Roles = $"{Roles.Customer}, {Roles.InstallerCompany}, {Roles.Admin}, {Roles.Distributor}, {Roles.Engineer}")]
        public async Task<IActionResult> RequiredDocuments(int id)
            => Ok(await _mediator.Send(new GetRequiredDocumentsQuery(id)));

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
