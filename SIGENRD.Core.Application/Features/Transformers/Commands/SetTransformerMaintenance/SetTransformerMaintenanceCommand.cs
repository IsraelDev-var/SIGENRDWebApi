using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.SetTransformerMaintenance
{
    /// <summary>
    /// Pone un transformador en mantenimiento o lo saca de él. En mantenimiento no admite
    /// solicitudes nuevas y su estado no se calcula (RN-02, RN-03).
    /// </summary>
    public class SetTransformerMaintenanceCommand : IRequest<Response<TransformerDetailDto>>
    {
        // Viene de la ruta
        public int Id { get; set; }

        public bool InMaintenance { get; set; }
    }
}
