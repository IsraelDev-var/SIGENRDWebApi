using FluentValidation;
using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.SearchAndTrack
{
    // ---------------- lista paginada con filtros ----------------

    /// <summary>Cola de trabajo y listado del solicitante: dentro de lo que cada rol puede ver (RN-22).</summary>
    public class SearchConnectionRequestsQuery : IRequest<Response<PagedDto<ConnectionRequestResponseDto>>>
    {
        public RequestStatus? Status { get; set; }
        public RequestType? RequestType { get; set; }
        public int? TransformerId { get; set; }

        /// <summary>Texto contenido en el código de expediente o en la dirección.</summary>
        public string? Text { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = PagedDto<ConnectionRequestResponseDto>.DefaultPageSize;
    }

    public class SearchConnectionRequestsValidator : AbstractValidator<SearchConnectionRequestsQuery>
    {
        public SearchConnectionRequestsValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
            RuleFor(q => q.PageSize).InclusiveBetween(1, PagedDto<ConnectionRequestResponseDto>.MaxPageSize)
                .WithMessage("El tamaño de página debe estar entre 1 y " + PagedDto<ConnectionRequestResponseDto>.MaxPageSize + ".");
            RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue).WithMessage("Estado inválido.");
            RuleFor(q => q.RequestType).IsInEnum().When(q => q.RequestType.HasValue).WithMessage("Tipo de solicitud inválido.");
            RuleFor(q => q.Text).MaximumLength(100).WithMessage("El texto de búsqueda no puede exceder los 100 caracteres.");
            RuleFor(q => q).Must(q => !q.From.HasValue || !q.To.HasValue || q.From <= q.To)
                .WithMessage("La fecha inicial no puede ser posterior a la final.");
        }
    }

    public class SearchConnectionRequestsHandler(IConnectionRequestRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<SearchConnectionRequestsQuery, Response<PagedDto<ConnectionRequestResponseDto>>>
    {
        public async Task<Response<PagedDto<ConnectionRequestResponseDto>>> Handle(SearchConnectionRequestsQuery request, CancellationToken cancellationToken)
        {
            var filter = new RequestSearchFilter(
                request.Status, request.RequestType, request.TransformerId, request.Text, request.From, request.To);

            var page = await repository.SearchAsync(
                RequestScopeResolver.For(currentUser), filter, request.Page, request.PageSize, cancellationToken);

            return new Response<PagedDto<ConnectionRequestResponseDto>>(new PagedDto<ConnectionRequestResponseDto>
            {
                Items = page.Items.Select(ConnectionRequestMapper.ToResponse).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            });
        }
    }

    // ---------------- seguimiento público por número de expediente (RF-14) ----------------

    /// <summary>
    /// Lo que cualquier persona con el número de expediente puede saber: nada personal, solo el estado.
    /// </summary>
    public class RequestTrackingDto
    {
        public string RequestCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    public record TrackRequestQuery(string Code) : IRequest<Response<RequestTrackingDto>>;

    public class TrackRequestValidator : AbstractValidator<TrackRequestQuery>
    {
        public TrackRequestValidator()
        {
            RuleFor(q => q.Code).NotEmpty().MaximumLength(50).WithMessage("El número de expediente es obligatorio.");
        }
    }

    public class TrackRequestHandler(IConnectionRequestRepository repository)
        : IRequestHandler<TrackRequestQuery, Response<RequestTrackingDto>>
    {
        public async Task<Response<RequestTrackingDto>> Handle(TrackRequestQuery request, CancellationToken cancellationToken)
        {
            var code = request.Code.Trim().ToUpperInvariant();

            var entity = await repository.GetByCodeAsync(code, cancellationToken)
                ?? throw new KeyNotFoundException("No existe un expediente con ese número.");

            return new Response<RequestTrackingDto>(new RequestTrackingDto
            {
                RequestCode = entity.RequestCode,
                RequestType = entity.RequestType.ToString(),
                Status = entity.Status.ToString(),
                RequestedAt = entity.RequestedAt,
                LastUpdatedAt = entity.UpdatedAt
            });
        }
    }
}
