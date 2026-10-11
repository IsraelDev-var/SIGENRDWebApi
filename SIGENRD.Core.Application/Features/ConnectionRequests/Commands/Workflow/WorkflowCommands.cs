using FluentValidation;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.Workflow
{
    // Comandos del flujo de estados (RN-12, RN-23). Cada uno es una transición; las reglas viven en
    // ConnectionRequest y RequestWorkflowService. El Id llega por la ruta.

    // ---------------- personal de la distribuidora ----------------

    /// <summary>Recibida a En revisión: valida documentos y reserva capacidad.</summary>
    public class StartReviewCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
    }

    /// <summary>En revisión a Observaciones. El comentario es obligatorio (RN-13).</summary>
    public class ObserveRequestCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
        public ObservationType ObservationType { get; set; } = ObservationType.Technical;
    }

    /// <summary>En revisión a Aprobada.</summary>
    public class ApproveRequestCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
    }

    /// <summary>En revisión a Rechazada. El comentario es obligatorio (RN-13).</summary>
    public class RejectRequestCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    // ---------------- solicitante ----------------

    /// <summary>Observaciones a En corrección.</summary>
    public class StartCorrectionCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
    }

    /// <summary>En corrección a En revisión.</summary>
    public class ResubmitRequestCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
    }

    /// <summary>Recibida, Observaciones o En corrección a Cancelada (RN-23).</summary>
    public class CancelRequestCommand : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
        public string? Comment { get; set; }
    }

    // ---------------- validación ----------------

    public class ObserveRequestValidator : AbstractValidator<ObserveRequestCommand>
    {
        public ObserveRequestValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0);

            RuleFor(c => c.Comment)
                .NotEmpty().WithMessage("Las observaciones deben incluir un comentario (RN-13).")
                .MaximumLength(500).WithMessage("El comentario no puede exceder los 500 caracteres.");

            RuleFor(c => c.ObservationType).IsInEnum().WithMessage("Tipo de observación inválido.");
        }
    }

    public class RejectRequestValidator : AbstractValidator<RejectRequestCommand>
    {
        public RejectRequestValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0);

            RuleFor(c => c.Comment)
                .NotEmpty().WithMessage("El rechazo debe incluir un comentario (RN-13).")
                .MaximumLength(500).WithMessage("El comentario no puede exceder los 500 caracteres.");
        }
    }

    public class CancelRequestValidator : AbstractValidator<CancelRequestCommand>
    {
        public CancelRequestValidator()
        {
            RuleFor(c => c.Comment).MaximumLength(500).WithMessage("El comentario no puede exceder los 500 caracteres.");
        }
    }

    // ---------------- handlers ----------------

    public class StartReviewHandler(RequestWorkflowService workflow) : IRequestHandler<StartReviewCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(StartReviewCommand request, CancellationToken ct)
            => new(await workflow.StartReviewAsync(request.Id, ct), "La solicitud pasó a revisión.");
    }

    public class ObserveRequestHandler(RequestWorkflowService workflow) : IRequestHandler<ObserveRequestCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(ObserveRequestCommand request, CancellationToken ct)
            => new(await workflow.ObserveAsync(request.Id, request.Comment.Trim(), request.ObservationType, ct), "Observaciones registradas.");
    }

    public class ApproveRequestHandler(RequestWorkflowService workflow) : IRequestHandler<ApproveRequestCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(ApproveRequestCommand request, CancellationToken ct)
            => new(await workflow.ApproveAsync(request.Id, ct), "La solicitud fue aprobada.");
    }

    public class RejectRequestHandler(RequestWorkflowService workflow) : IRequestHandler<RejectRequestCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(RejectRequestCommand request, CancellationToken ct)
            => new(await workflow.RejectAsync(request.Id, request.Comment.Trim(), ct), "La solicitud fue rechazada.");
    }

    public class StartCorrectionHandler(RequestWorkflowService workflow) : IRequestHandler<StartCorrectionCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(StartCorrectionCommand request, CancellationToken ct)
            => new(await workflow.StartCorrectionAsync(request.Id, ct), "La solicitud pasó a corrección.");
    }

    public class ResubmitRequestHandler(RequestWorkflowService workflow) : IRequestHandler<ResubmitRequestCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(ResubmitRequestCommand request, CancellationToken ct)
            => new(await workflow.ResubmitAsync(request.Id, ct), "La solicitud volvió a revisión.");
    }

    public class CancelRequestHandler(RequestWorkflowService workflow) : IRequestHandler<CancelRequestCommand, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(CancelRequestCommand request, CancellationToken ct)
            => new(await workflow.CancelAsync(request.Id, request.Comment?.Trim(), ct), "La solicitud fue cancelada.");
    }
}
