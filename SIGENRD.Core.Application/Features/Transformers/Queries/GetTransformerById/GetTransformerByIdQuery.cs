using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformerById
{
    public record GetTransformerByIdQuery(int Id) : IRequest<Response<TransformerDetailDto>>;
}
