using AutoMapper;
using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;


namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetAllConnectionRequests
{
    public class GetAllConnectionRequestsHandler : IRequestHandler<GetAllConnectionRequestsQuery, Response<List<ConnectionRequestResponseDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllConnectionRequestsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Response<List<ConnectionRequestResponseDto>>> Handle(GetAllConnectionRequestsQuery request, CancellationToken cancellationToken)
        {
            var list = await _unitOfWork.Repository<ConnectionRequest>().GetAllAsync();
            var listDto = _mapper.Map<List<ConnectionRequestResponseDto>>(list);

            return new Response<List<ConnectionRequestResponseDto>>(listDto);
        }
    }
}
