using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetRequiredDocuments
{
    /// <summary>Qué documentos exige una solicitud y cuáles ya están presentes (RN-09, RN-11).</summary>
    public record GetRequiredDocumentsQuery(int Id) : IRequest<Response<RequiredDocumentsDto>>;

    public class RequiredDocumentsDto
    {
        public bool Complete { get; set; }
        public List<RequiredDocumentItemDto> Items { get; set; } = new();
    }

    public class RequiredDocumentItemDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Cualquiera de estos tipos cumple el requisito.</summary>
        public List<string> AcceptedTypes { get; set; } = new();

        public bool Satisfied { get; set; }
    }

    public class GetRequiredDocumentsHandler(IConnectionRequestRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<GetRequiredDocumentsQuery, Response<RequiredDocumentsDto>>
    {
        public async Task<Response<RequiredDocumentsDto>> Handle(GetRequiredDocumentsQuery request, CancellationToken cancellationToken)
        {
            // Solo quien puede ver la solicitud ve qué le falta (RN-22).
            var entity = await repository.GetForUpdateAsync(request.Id, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + request.Id + " no encontrada.");

            var present = (entity.TechnicalDocuments ?? new List<TechnicalDocument>()).Select(d => d.DocumentType).ToHashSet();
            var requirements = RequiredDocuments.For(entity, entity.Customer?.IsLegalEntity ?? false);

            var items = requirements.Select(r => new RequiredDocumentItemDto
            {
                Code = r.Code,
                Description = r.Description,
                AcceptedTypes = r.AnyOf.Select(t => t.ToString()).ToList(),
                Satisfied = r.IsSatisfiedBy(present)
            }).ToList();

            return new Response<RequiredDocumentsDto>(new RequiredDocumentsDto
            {
                Items = items,
                Complete = items.All(i => i.Satisfied)
            });
        }
    }
}
