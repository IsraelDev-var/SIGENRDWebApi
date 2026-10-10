using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.UpdateTransformerCapacity
{
    /// <summary>
    /// La distribuidora informa la capacidad vigente de un transformador. El estado se recalcula (RN-02).
    /// </summary>
    public class UpdateTransformerCapacityCommand : IRequest<Response<TransformerDetailDto>>
    {
        // Viene de la ruta, no del cuerpo
        public int Id { get; set; }

        public decimal TotalCapacityKva { get; set; }
        public decimal AvailableCapacityKva { get; set; }
    }
}
