using MediatR;
using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Queries.GetConnectionRequestById
{
    public class GetConnectionRequestByIdQuery : IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }

        public GetConnectionRequestByIdQuery(int id)
        {
            Id = id;
        }
    }
}
