
using AutoMapper;
using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetAllConnectionRequests;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Interfaces;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetConnectionRequestById
{
    public class GetConnectionRequestByIdHandler : IRequestHandler<GetConnectionRequestByIdQuery, Response<ConnectionRequestResponseDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetConnectionRequestByIdHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Response<ConnectionRequestResponseDto>> Handle(GetConnectionRequestByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _unitOfWork.Repository<ConnectionRequest>().GetByIdAsync(request.Id);

            if (entity == null)
                return new Response<ConnectionRequestResponseDto>("Solicitud no encontrada.");

            var dto = _mapper.Map<ConnectionRequestResponseDto>(entity);
            return new Response<ConnectionRequestResponseDto>(dto);
        }
    }
}
