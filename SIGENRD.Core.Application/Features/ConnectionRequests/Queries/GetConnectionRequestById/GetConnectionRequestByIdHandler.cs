using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetConnectionRequestById
{
    public class GetConnectionRequestByIdHandler(IConnectionRequestRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<GetConnectionRequestByIdQuery, Response<ConnectionRequestResponseDto>>
    {
        public async Task<Response<ConnectionRequestResponseDto>> Handle(GetConnectionRequestByIdQuery request, CancellationToken cancellationToken)
        {
            var scope = RequestScopeResolver.For(currentUser);

            // Una solicitud ajena responde igual que una inexistente: no se revela que existe (RN-22).
            var entity = await repository.GetVisibleByIdAsync(request.Id, scope, cancellationToken)
                ?? throw new KeyNotFoundException("Solicitud " + request.Id + " no encontrada.");

            return new Response<ConnectionRequestResponseDto>(ConnectionRequestMapper.ToResponse(entity));
        }
    }
}
