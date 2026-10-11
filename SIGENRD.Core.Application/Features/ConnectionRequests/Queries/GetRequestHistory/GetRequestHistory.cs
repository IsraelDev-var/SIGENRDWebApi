using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetRequestHistory
{
    /// <summary>Historial de cambios de estado de una solicitud (RN-10).</summary>
    public record GetRequestHistoryQuery(int Id) : IRequest<Response<List<StateHistoryDto>>>;

    public class GetRequestHistoryHandler(IConnectionRequestRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<GetRequestHistoryQuery, Response<List<StateHistoryDto>>>
    {
        public async Task<Response<List<StateHistoryDto>>> Handle(GetRequestHistoryQuery request, CancellationToken cancellationToken)
        {
            // Solo quien puede ver la solicitud puede ver su historial (RN-22).
            _ = await repository.GetVisibleByIdAsync(request.Id, RequestScopeResolver.For(currentUser), cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + request.Id + " no encontrada.");

            var history = await repository.GetHistoryAsync(request.Id, cancellationToken);

            var dtos = history.Select(h => new StateHistoryDto
            {
                Id = h.Id,
                PreviousState = h.PreviousState,
                NewState = h.NewState,
                Comment = h.Comment,
                CreatedAt = h.CreatedAt,
                CreatedBy = h.CreatedBy
            }).ToList();

            return new Response<List<StateHistoryDto>>(dtos);
        }
    }
}
