using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformerById
{
    public class GetTransformerByIdHandler(ITransformerRepository repository)
        : IRequestHandler<GetTransformerByIdQuery, Response<TransformerDetailDto>>
    {
        public async Task<Response<TransformerDetailDto>> Handle(GetTransformerByIdQuery request, CancellationToken cancellationToken)
        {
            var transformer = await repository.GetByIdAsync(request.Id, asTracking: false, cancellationToken)
                ?? throw new KeyNotFoundException("Transformador " + request.Id + " no encontrado.");

            return new Response<TransformerDetailDto>(TransformerMapper.ToDetail(transformer));
        }
    }
}
