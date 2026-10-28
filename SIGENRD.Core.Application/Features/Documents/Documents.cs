using FluentValidation;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Exceptions;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.Core.Application.Features.Documents
{
    // Gestión documental del expediente (RF-16, RF-34 a RF-37, RN-18, RN-19, RN-22).

    public class DocumentDto
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }

        public static DocumentDto From(TechnicalDocument d) => new()
        {
            Id = d.Id,
            DocumentType = d.DocumentType.ToString(),
            FileName = d.FileName,
            ContentType = d.ContentType,
            SizeBytes = d.SizeBytes,
            UploadedAt = d.CreatedAt
        };
    }

    /// <summary>Archivo listo para descargar: el controlador lo transmite tal cual.</summary>
    public record DocumentFile(string FileName, string ContentType, Stream Content);

    internal static class DocumentKeys
    {
        /// <summary>Clave interna del almacén: la genera la aplicación, nunca el nombre que manda el usuario.</summary>
        public static string For(int requestId, string extension) =>
            "requests/" + requestId + "/" + Guid.NewGuid().ToString("N") + extension;
    }

    // ---------------- subir ----------------

    public class UploadDocumentCommand : IRequest<Response<DocumentDto>>
    {
        public int RequestId { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }

    public class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
    {
        public UploadDocumentValidator()
        {
            RuleFor(c => c.DocumentType).IsInEnum().WithMessage("Tipo de documento inválido.")
                .NotEqual(DocumentType.ApprovalCertificate).WithMessage("El certificado de aprobación lo genera el sistema.");
            RuleFor(c => c.FileName).NotEmpty().WithMessage("El archivo debe tener nombre.");
            RuleFor(c => c.Content.Length)
                .GreaterThan(0).WithMessage("El archivo está vacío.")
                .LessThanOrEqualTo((int)DocumentFileInspector.MaxSizeBytes).WithMessage("El archivo supera el máximo de 10 MB.");
        }
    }

    public class UploadDocumentHandler(
        IConnectionRequestRepository requests, IUnitOfWork unitOfWork, IFileStorage storage, ICurrentUserService currentUser)
        : IRequestHandler<UploadDocumentCommand, Response<DocumentDto>>
    {
        public async Task<Response<DocumentDto>> Handle(UploadDocumentCommand command, CancellationToken cancellationToken)
        {
            var request = await requests.GetForUpdateAsync(command.RequestId, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + command.RequestId + " no encontrada.");

            if (!RequestWorkflow.AllowsDocumentChanges(request.Status))
                throw new DocumentsLockedException(request.Status);

            var file = DocumentFileInspector.Inspect(command.FileName, command.Content);
            var key = DocumentKeys.For(request.Id, file.Extension);

            using (var content = new MemoryStream(command.Content, writable: false))
                await storage.SaveAsync(key, content, file.ContentType, cancellationToken);

            var document = new TechnicalDocument
            {
                ConnectionRequestId = request.Id,
                DocumentType = command.DocumentType,
                FileName = file.SafeFileName,
                ContentType = file.ContentType,
                SizeBytes = command.Content.Length,
                FileUrl = key
            };

            try
            {
                // La carga queda auditada (RN-20) al guardarse el documento.
                await unitOfWork.Repository<TechnicalDocument>().AddAsync(document);
                await unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Sin registro en la base, el archivo no sirve: se retira para no dejar huérfanos.
                await storage.DeleteAsync(key, CancellationToken.None);
                throw;
            }

            return new Response<DocumentDto>(DocumentDto.From(document));
        }
    }

    // ---------------- permiso de subida directa ----------------

    public class DocumentUploadTicketDto
    {
        /// <summary>Se manda como "Authorization: Bearer ..." al subir. Vale una vez.</summary>
        public string Ticket { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        /// <summary>Ruta de la API donde se sube el archivo con el permiso.</summary>
        public string UploadPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pide un permiso para que el navegador suba el archivo directo a la API. Se comprueba aquí lo mismo que al subir
    /// (RN-22, RN-19 y el tipo), para no emitir permisos que de todas formas se rechazarían.
    /// </summary>
    public record CreateDocumentUploadTicketCommand(int RequestId, DocumentType DocumentType) : IRequest<Response<DocumentUploadTicketDto>>;

    public class CreateDocumentUploadTicketValidator : AbstractValidator<CreateDocumentUploadTicketCommand>
    {
        public CreateDocumentUploadTicketValidator()
        {
            RuleFor(c => c.DocumentType).IsInEnum().WithMessage("Tipo de documento inválido.")
                .NotEqual(DocumentType.ApprovalCertificate).WithMessage("El certificado de aprobación lo genera el sistema.");
        }
    }

    public class CreateDocumentUploadTicketHandler(
        IConnectionRequestRepository requests, IDocumentUploadTickets tickets, ICurrentUserService currentUser)
        : IRequestHandler<CreateDocumentUploadTicketCommand, Response<DocumentUploadTicketDto>>
    {
        public async Task<Response<DocumentUploadTicketDto>> Handle(CreateDocumentUploadTicketCommand command, CancellationToken cancellationToken)
        {
            var request = await requests.GetForUpdateAsync(command.RequestId, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + command.RequestId + " no encontrada.");

            if (!RequestWorkflow.AllowsDocumentChanges(request.Status))
                throw new DocumentsLockedException(request.Status);

            // Solo el solicitante sube documentos (matriz 3.2): el controlador ya limita a estos dos roles.
            var role = currentUser.IsInRole(nameof(UserType.Customer)) ? nameof(UserType.Customer) : nameof(UserType.InstallerCompany);
            var ticket = tickets.Issue(currentUser.UserId!, currentUser.FullName ?? string.Empty, role, request.Id, command.DocumentType);

            return new Response<DocumentUploadTicketDto>(new DocumentUploadTicketDto
            {
                Ticket = ticket.Ticket,
                ExpiresAt = ticket.ExpiresAt,
                UploadPath = "/api/v1/ConnectionRequest/" + request.Id + "/documents/direct"
            });
        }
    }

    // ---------------- listar ----------------

    public record ListDocumentsQuery(int RequestId) : IRequest<Response<List<DocumentDto>>>;

    public class ListDocumentsHandler(IConnectionRequestRepository requests, ICurrentUserService currentUser)
        : IRequestHandler<ListDocumentsQuery, Response<List<DocumentDto>>>
    {
        public async Task<Response<List<DocumentDto>>> Handle(ListDocumentsQuery query, CancellationToken cancellationToken)
        {
            var documents = await requests.GetDocumentsAsync(query.RequestId, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + query.RequestId + " no encontrada.");

            return new Response<List<DocumentDto>>(documents.Select(DocumentDto.From).ToList());
        }
    }

    // ---------------- descargar ----------------

    public record DownloadDocumentQuery(int RequestId, int DocumentId) : IRequest<DocumentFile>;

    public class DownloadDocumentHandler(IConnectionRequestRepository requests, IFileStorage storage, ICurrentUserService currentUser)
        : IRequestHandler<DownloadDocumentQuery, DocumentFile>
    {
        public async Task<DocumentFile> Handle(DownloadDocumentQuery query, CancellationToken cancellationToken)
        {
            // Un documento de una solicitud ajena responde igual que uno inexistente (RN-22).
            var document = await requests.GetDocumentAsync(query.RequestId, query.DocumentId, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Documento " + query.DocumentId + " no encontrado.");

            var content = await storage.OpenReadAsync(document.FileUrl, cancellationToken)
                ?? throw new KeyNotFoundException("El archivo del documento " + query.DocumentId + " no está en el almacén.");

            return new DocumentFile(document.FileName, document.ContentType, content);
        }
    }

    /// <summary>El certificado de aprobación vigente de la solicitud (RF-17, RF-37).</summary>
    public record DownloadCertificateQuery(int RequestId) : IRequest<DocumentFile>;

    public class DownloadCertificateHandler(IConnectionRequestRepository requests, IMediator mediator, ICurrentUserService currentUser)
        : IRequestHandler<DownloadCertificateQuery, DocumentFile>
    {
        public async Task<DocumentFile> Handle(DownloadCertificateQuery query, CancellationToken cancellationToken)
        {
            var documents = await requests.GetDocumentsAsync(query.RequestId, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + query.RequestId + " no encontrada.");

            var certificate = documents.LastOrDefault(d => d.DocumentType == DocumentType.ApprovalCertificate)
                ?? throw new KeyNotFoundException("La solicitud " + query.RequestId + " no tiene certificado de aprobación.");

            return await mediator.Send(new DownloadDocumentQuery(query.RequestId, certificate.Id), cancellationToken);
        }
    }

    // ---------------- retirar ----------------

    public record DeleteDocumentCommand(int RequestId, int DocumentId) : IRequest<Response<int>>;

    public class DeleteDocumentHandler(IConnectionRequestRepository requests, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
        : IRequestHandler<DeleteDocumentCommand, Response<int>>
    {
        public async Task<Response<int>> Handle(DeleteDocumentCommand command, CancellationToken cancellationToken)
        {
            var request = await requests.GetForUpdateAsync(command.RequestId, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + command.RequestId + " no encontrada.");

            var document = request.TechnicalDocuments?.FirstOrDefault(d => d.Id == command.DocumentId)
                ?? throw new KeyNotFoundException("Documento " + command.DocumentId + " no encontrado.");

            if (document.DocumentType == DocumentType.ApprovalCertificate)
                throw new ApiException("El certificado de aprobación no se puede retirar.");

            // RN-19: tras la aprobación (y fuera de Recibida o En corrección) los documentos quedan fijos.
            if (!RequestWorkflow.AllowsDocumentChanges(request.Status))
                throw new DocumentsLockedException(request.Status);

            // Baja lógica: el archivo y su registro se conservan para el historial; la auditoría registra la baja (RN-20).
            document.IsActive = false;
            await unitOfWork.SaveChangesAsync();

            return new Response<int>(document.Id);
        }
    }
}
