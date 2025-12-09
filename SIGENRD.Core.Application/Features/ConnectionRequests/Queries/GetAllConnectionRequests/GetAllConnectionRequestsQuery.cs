using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Wrappers;


namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetAllConnectionRequests
{
    // Solicitamos una lista de DTOs de lectura
    public class GetAllConnectionRequestsQuery : IRequest<Response<List<ConnectionRequestResponseDto>>>
    {
        // Aquí podrías poner filtros en el futuro: public int PageNumber { get; set; }
    }
}
