using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.ImportTransformers
{
    /// <summary>
    /// Carga masiva de transformadores desde la plantilla CSV de la distribuidora.
    /// El flujo es todo o nada: si una fila falla, no se importa ninguna.
    /// </summary>
    public class ImportTransformersCommand : IRequest<Response<ImportTransformersResultDto>>
    {
        public int DistributorId { get; set; }

        // El llamador es dueño del flujo y lo cierra
        public Stream Content { get; set; } = Stream.Null;
    }
}
