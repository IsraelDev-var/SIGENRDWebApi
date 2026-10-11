using FluentValidation;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Audit
{
    /// <summary>Consulta del registro de auditoría (RN-20), paginada y con filtros. Solo administradores.</summary>
    public class SearchAuditLogsQuery : IRequest<Response<PagedDto<AuditLogDto>>>
    {
        /// <summary>Entidad auditada, por ejemplo ConnectionRequest, TechnicalDocument o Account (inicios de sesión).</summary>
        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public string? UserId { get; set; }

        /// <summary>Added, Modified, Deleted o un evento de seguridad (LoginFailed, LoginLockedOut...).</summary>
        public string? Action { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = PagedDto<AuditLogDto>.DefaultPageSize;
    }

    public class AuditLogDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTime CreatedAt { get; set; }

        public static AuditLogDto From(AuditLog a) => new()
        {
            Id = a.Id,
            UserId = a.UserId,
            Action = a.Action,
            EntityName = a.EntityName,
            EntityId = a.EntityId,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            CreatedAt = a.CreatedAt
        };
    }

    public class SearchAuditLogsValidator : AbstractValidator<SearchAuditLogsQuery>
    {
        public SearchAuditLogsValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
            RuleFor(q => q.PageSize).InclusiveBetween(1, PagedDto<AuditLogDto>.MaxPageSize)
                .WithMessage("El tamaño de página debe estar entre 1 y " + PagedDto<AuditLogDto>.MaxPageSize + ".");
            RuleFor(q => q.EntityName).MaximumLength(100);
            RuleFor(q => q.EntityId).MaximumLength(100);
            RuleFor(q => q.UserId).MaximumLength(100);
            RuleFor(q => q.Action).MaximumLength(50);
            RuleFor(q => q).Must(q => !q.From.HasValue || !q.To.HasValue || q.From <= q.To)
                .WithMessage("La fecha inicial no puede ser posterior a la final.");
        }
    }

    public class SearchAuditLogsHandler(IAuditLogRepository auditLogs)
        : IRequestHandler<SearchAuditLogsQuery, Response<PagedDto<AuditLogDto>>>
    {
        public async Task<Response<PagedDto<AuditLogDto>>> Handle(SearchAuditLogsQuery query, CancellationToken cancellationToken)
        {
            var filter = new AuditLogFilter(query.EntityName, query.EntityId, query.UserId, query.Action, query.From, query.To);
            var page = await auditLogs.SearchAsync(filter, query.Page, query.PageSize, cancellationToken);

            return new Response<PagedDto<AuditLogDto>>(new PagedDto<AuditLogDto>
            {
                Items = page.Items.Select(AuditLogDto.From).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            });
        }
    }
}
