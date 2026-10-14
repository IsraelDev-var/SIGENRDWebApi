using FluentValidation;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.ListTransformersForManagement
{
    /// <summary>
    /// Lista paginada de la red para la distribuidora (B-3): buscar por código, filtrar por zona y estado, y ordenar
    /// por los que necesitan atención. El mapa no sirve para esto: pide un área y no pagina.
    /// </summary>
    public class ListTransformersForManagementQuery : IRequest<Response<PagedDto<TransformerDetailDto>>>
    {
        /// <summary>Texto contenido en el código.</summary>
        public string? Code { get; set; }
        public int? ServiceZoneId { get; set; }
        public TransformerStatus? Status { get; set; }
        public TransformerSort Sort { get; set; } = TransformerSort.Code;

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = PagedDto<TransformerDetailDto>.DefaultPageSize;
    }

    public class ListTransformersForManagementValidator : AbstractValidator<ListTransformersForManagementQuery>
    {
        public ListTransformersForManagementValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");
            RuleFor(q => q.PageSize).InclusiveBetween(1, PagedDto<TransformerDetailDto>.MaxPageSize)
                .WithMessage("El tamaño de página debe estar entre 1 y " + PagedDto<TransformerDetailDto>.MaxPageSize + ".");
            RuleFor(q => q.Code).MaximumLength(50).WithMessage("El código no puede exceder los 50 caracteres.");
            RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue).WithMessage("Estado inválido.");
            RuleFor(q => q.Sort).IsInEnum().WithMessage("Orden inválido.");
            RuleFor(q => q.ServiceZoneId).GreaterThan(0).When(q => q.ServiceZoneId.HasValue).WithMessage("Zona inválida.");
        }
    }

    public class ListTransformersForManagementHandler(ITransformerRepository repository)
        : IRequestHandler<ListTransformersForManagementQuery, Response<PagedDto<TransformerDetailDto>>>
    {
        public async Task<Response<PagedDto<TransformerDetailDto>>> Handle(ListTransformersForManagementQuery query, CancellationToken cancellationToken)
        {
            var page = await repository.SearchForManagementAsync(
                new TransformerManagementFilter(query.Code, query.ServiceZoneId, query.Status, query.Sort),
                query.Page, query.PageSize, cancellationToken);

            return new Response<PagedDto<TransformerDetailDto>>(new PagedDto<TransformerDetailDto>
            {
                Items = page.Items.Select(TransformerMapper.ToDetail).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount
            });
        }
    }
}
