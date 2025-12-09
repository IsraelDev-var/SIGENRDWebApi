using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Observaciones o comentarios técnicos/documentales en el proceso de revisión de una solicitud.
    /// </summary>
    public class ReviewObservation : AuditableEntity
    {
        public int Id { get; set; }

        // Id de la solicitud relacionada
        public int ConnectionRequestId { get; set; }

        // Comentario del revisor
        public string Comment { get; set; } = string.Empty;

        // Tipo de observación (Técnica / Documental)
        public ObservationType DocumentType { get; set; } = ObservationType.Technical;

       

        // Relación
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
