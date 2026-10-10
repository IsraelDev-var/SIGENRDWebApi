using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Services;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetAllConnectionRequests
{
    public class GetAllConnectionRequestsHandler(IConnectionRequestRepository repository, ICurrentUserService currentUser)
        : IRequestHandler<GetAllConnectionRequestsQuery, Response<List<ConnectionRequestResponseDto>>>
    {
        public async Task<Response<List<ConnectionRequestResponseDto>>> Handle(GetAllConnectionRequestsQuery request, CancellationToken cancellationToken)
        {
            // RN-22: cada usuario recibe solo las solicitudes que le corresponden.
            var scope = RequestScopeResolver.For(currentUser);
            var list = await repository.GetVisibleAsync(scope, cancellationToken);

            return new Response<List<ConnectionRequestResponseDto>>(list.Select(ConnectionRequestMapper.ToResponse).ToList());
        }
    }
}
