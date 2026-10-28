using Microsoft.Extensions.Logging;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Exceptions;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.Core.Application.Services
{
    /// <summary>
    /// Ejecuta las transiciones del flujo de una solicitud (RN-12, RN-23): revisar, observar, corregir,
    /// reenviar, aprobar, rechazar y cancelar. Las reglas de estado y de capacidad viven en el dominio;
    /// aquí se carga la solicitud, se aplica la regla y se deja el rastro (historial, observación y avisos).
    /// </summary>
    public class RequestWorkflowService(
        IConnectionRequestRepository requests,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IEmailService emailService,
        StaffNotifier staffNotifier,
        IFileStorage storage,
        ICertificateGenerator certificates,
        ILogger<RequestWorkflowService> logger)
    {
        // ---------------- personal de la distribuidora ----------------

        public async Task<ConnectionRequestResponseDto> StartReviewAsync(int id, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;
            var (available, conditioned) = Thresholds(request);

            EnsureDocuments(request);
            request.StartReview(request.Transformer!, available, conditioned);

            return await SaveAsync(request, from, null, null, ct);
        }

        public async Task<ConnectionRequestResponseDto> ObserveAsync(int id, string comment, ObservationType type, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;

            request.Observe();

            return await SaveAsync(request, from, comment, type, ct);
        }

        public async Task<ConnectionRequestResponseDto> ApproveAsync(int id, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;
            var (available, conditioned) = Thresholds(request);

            EnsureDocuments(request);

            try
            {
                request.Approve(request.Transformer!, available, conditioned);
            }
            catch (InsufficientCapacityException ex)
            {
                // RN-03: si la capacidad cayó mientras se revisaba, no se aprueba y vuelve a Observaciones.
                request.Observe();
                await SaveAsync(request, from, "Capacidad insuficiente al aprobar: " + ex.Message, ObservationType.Technical, ct);
                throw;
            }

            var approvedAt = DateTime.UtcNow;
            if (request.NetMeteringRequest is not null)
                request.NetMeteringRequest.ApprovedAt = approvedAt;

            // RN-14: la aprobación genera el certificado y lo guarda en el expediente, en el mismo guardado.
            var certificateKey = await IssueCertificateAsync(request, approvedAt, ct);
            try
            {
                return await SaveAsync(request, from, null, null, ct);
            }
            catch
            {
                // Si la aprobación no se guarda, el certificado no debe quedar en el almacén.
                await storage.DeleteAsync(certificateKey, CancellationToken.None);
                throw;
            }
        }

        public async Task<ConnectionRequestResponseDto> RejectAsync(int id, string comment, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;
            var (available, conditioned) = Thresholds(request);

            request.Reject(request.Transformer!, available, conditioned);

            return await SaveAsync(request, from, comment, ObservationType.Administrative, ct);
        }

        // ---------------- solicitante ----------------

        /// <summary>
        /// Borrador a Recibida (D-4): con los documentos completos (RN-09, RN-11) y el transformador todavía
        /// admitiendo solicitudes (RN-03). Recién aquí se avisa a la distribuidora y al solicitante (RN-16).
        /// </summary>
        public async Task<ConnectionRequestResponseDto> SubmitAsync(int id, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;

            if (request.Status == RequestStatus.Draft && !request.Transformer!.AcceptsNewRequests)
                throw new ApiException("El transformador " + request.Transformer.Code + " ya no admite solicitudes nuevas (" +
                                       request.Transformer.Status + "). Elige otro transformador en el borrador.");

            EnsureDocuments(request);
            request.Submit();

            await staffNotifier.NotifyAsync(
                "Nueva solicitud " + request.RequestCode,
                "Llegó una solicitud de " + (request.RequestType == RequestType.NetMetering ? "medición neta" : "conexión") +
                " por " + request.RequestedCapacityKva.ToString("0.##") + " kVA en el transformador " + request.Transformer!.Code + ".",
                NotificationType.NewRequestForReview, request.DistributorId, ct);

            return await SaveAsync(request, from, "Solicitud enviada", null, ct);
        }

        public async Task<ConnectionRequestResponseDto> StartCorrectionAsync(int id, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;

            request.StartCorrection();

            return await SaveAsync(request, from, null, null, ct);
        }

        public async Task<ConnectionRequestResponseDto> ResubmitAsync(int id, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;

            // RN-11: los documentos mínimos se revalidan al reenviar.
            EnsureDocuments(request);
            request.Resubmit();

            // El personal debe saber que la solicitud volvió a su bandeja.
            await staffNotifier.NotifyAsync(
                "Solicitud " + request.RequestCode + " reenviada",
                "El solicitante atendió las observaciones de la solicitud " + request.RequestCode + " y espera nueva revisión.",
                NotificationType.NewRequestForReview, request.DistributorId, ct);

            return await SaveAsync(request, from, null, null, ct);
        }

        public async Task<ConnectionRequestResponseDto> CancelAsync(int id, string? comment, CancellationToken ct)
        {
            var request = await LoadAsync(id, RequestScopeResolver.For(currentUser), ct);
            var from = request.Status;
            var (available, conditioned) = Thresholds(request);

            request.Cancel(request.Transformer!, available, conditioned);

            // Descartar un borrador no es noticia para nadie: queda en el historial, sin avisos.
            return await SaveAsync(request, from, comment ?? (from == RequestStatus.Draft ? "Borrador descartado" : null), null, ct,
                notify: from != RequestStatus.Draft);
        }

        // ---------------- apoyo ----------------

        private async Task<ConnectionRequest> LoadAsync(int id, RequestScope scope, CancellationToken ct)
        {
            // Una solicitud ajena responde igual que una inexistente (RN-22).
            return await requests.GetForUpdateAsync(id, scope, ct)
                ?? throw new KeyNotFoundException("Solicitud " + id + " no encontrada.");
        }

        /// <summary>Genera el PDF del certificado, lo guarda en el almacén y lo agrega como documento. Devuelve su clave.</summary>
        private async Task<string> IssueCertificateAsync(ConnectionRequest request, DateTime approvedAt, CancellationToken ct)
        {
            var pdf = certificates.Generate(new ApprovalCertificateData(
                RequestCode: request.RequestCode,
                RequestType: request.RequestType == RequestType.NetMetering ? "Medición neta" : "Conexión",
                ApplicantName: request.Customer?.FullName ?? request.InstallerCompany?.TradeName ?? "—",
                ProjectAddress: request.ProjectAddress,
                DistributorName: request.Transformer!.Distributor!.Name,
                TransformerCode: request.Transformer.Code,
                ApprovedCapacityKva: request.RequestedCapacityKva,
                ApprovedAt: approvedAt,
                ReviewerName: currentUser.FullName ?? currentUser.UserId ?? "—"));

            var key = "requests/" + request.Id + "/certificado-" + Guid.NewGuid().ToString("N") + ".pdf";
            using (var content = new MemoryStream(pdf, writable: false))
                await storage.SaveAsync(key, content, "application/pdf", ct);

            await unitOfWork.Repository<TechnicalDocument>().AddAsync(new TechnicalDocument
            {
                ConnectionRequestId = request.Id,
                DocumentType = DocumentType.ApprovalCertificate,
                FileName = "Certificado-" + request.RequestCode + ".pdf",
                ContentType = "application/pdf",
                SizeBytes = pdf.Length,
                FileUrl = key
            });

            return key;
        }

        private static (decimal Available, decimal Conditioned) Thresholds(ConnectionRequest request)
        {
            var distributor = request.Transformer!.Distributor!;
            return (distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);
        }

        private static void EnsureDocuments(ConnectionRequest request)
        {
            var present = (request.TechnicalDocuments ?? new List<TechnicalDocument>()).Select(d => d.DocumentType);
            var missing = RequiredDocuments.Missing(request, request.Customer?.IsLegalEntity ?? false, present);

            if (missing.Count > 0)
                throw new ApiException("Faltan documentos obligatorios: " +
                                       string.Join("; ", missing.Select(m => m.Description)) + ".");
        }

        /// <summary>
        /// Deja el rastro del cambio (RN-10), avisa al solicitante dentro del portal (RN-17) y guarda todo en una
        /// sola transacción. El correo sale después y su fallo no deshace la transición.
        /// </summary>
        private async Task<ConnectionRequestResponseDto> SaveAsync(
            ConnectionRequest request, RequestStatus from, string? comment, ObservationType? observation, CancellationToken ct,
            bool notify = true)
        {
            await unitOfWork.Repository<StateHistory>().AddAsync(new StateHistory
            {
                ConnectionRequestId = request.Id,
                PreviousState = from.ToString(),
                NewState = request.Status.ToString(),
                Comment = comment
            });

            if (observation.HasValue && !string.IsNullOrWhiteSpace(comment))
            {
                await unitOfWork.Repository<ReviewObservation>().AddAsync(new ReviewObservation
                {
                    ConnectionRequestId = request.Id,
                    Comment = comment,
                    ObservationType = observation.Value
                });
            }

            var (title, message, type) = Describe(request, comment);
            var recipients = notify ? Recipients(request) : new List<(string? UserId, string? Email)>();

            foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r.UserId)).DistinctBy(r => r.UserId))
            {
                await unitOfWork.Repository<Notification>().AddAsync(new Notification
                {
                    UserId = recipient.UserId!,
                    Title = title,
                    Message = message,
                    NotificationType = type
                });
            }

            await unitOfWork.SaveChangesAsync();

            foreach (var email in recipients.Select(r => r.Email).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct())
            {
                try
                {
                    await emailService.SendAsync(new EmailRequest
                    {
                        To = email!,
                        Subject = title,
                        Body = "<p>" + System.Net.WebUtility.HtmlEncode(message) + "</p>"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "No se pudo enviar el aviso de la solicitud {Code} a {Email}", request.RequestCode, email);
                }
            }

            return ConnectionRequestMapper.ToResponse(request);
        }

        private static List<(string? UserId, string? Email)> Recipients(ConnectionRequest request)
        {
            var list = new List<(string?, string?)>();

            if (request.Customer is not null)
                list.Add((request.Customer.ApplicationUserId, request.Customer.Email));

            if (request.InstallerCompany is not null)
                list.Add((request.InstallerCompany.ApplicationUserId, request.InstallerCompany.ContactEmail));

            return list;
        }

        private static (string Title, string Message, NotificationType Type) Describe(ConnectionRequest request, string? comment)
        {
            var code = request.RequestCode;
            var detail = string.IsNullOrWhiteSpace(comment) ? string.Empty : " Comentario: " + comment;

            return request.Status switch
            {
                RequestStatus.Received => ("Solicitud " + code + " recibida",
                    "Recibimos la solicitud " + code + ". Puedes darle seguimiento con ese número.", NotificationType.RequestReceived),
                RequestStatus.UnderReview => ("Solicitud " + code + ": en revisión",
                    "Tu solicitud " + code + " está en revisión.", NotificationType.StatusChanged),
                RequestStatus.Observed => ("Solicitud " + code + ": observaciones",
                    "Tu solicitud " + code + " tiene observaciones por atender." + detail, NotificationType.ObservationAdded),
                RequestStatus.InCorrection => ("Solicitud " + code + ": en corrección",
                    "Tu solicitud " + code + " pasó a corrección.", NotificationType.StatusChanged),
                RequestStatus.Approved => ("Solicitud " + code + ": aprobada",
                    "Tu solicitud " + code + " fue aprobada. El certificado de aprobación está disponible en el expediente.",
                    NotificationType.RequestApproved),
                RequestStatus.Rejected => ("Solicitud " + code + ": rechazada",
                    "Tu solicitud " + code + " fue rechazada." + detail, NotificationType.RequestRejected),
                RequestStatus.Cancelled => ("Solicitud " + code + ": cancelada",
                    "La solicitud " + code + " fue cancelada.", NotificationType.StatusChanged),
                _ => ("Solicitud " + code, "Tu solicitud " + code + " cambió de estado.", NotificationType.StatusChanged)
            };
        }
    }
}
