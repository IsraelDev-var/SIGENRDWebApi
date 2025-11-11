

using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un documento técnico adjunto a una solicitud de conexión.
    /// </summary>
    public class TechnicalDocument : AuditableEntity
    {
        public int Id { get; set; }

        // Id de la solicitud asociada
        public int ConnectionRequestId { get; set; }

        // Nombre del archivo
        public string FileName { get; set; } = string.Empty;

        // Tipo de documento (Plano, Permiso, Ficha técnica)
        public DocumentType DocumentType { get; set; } = DocumentType.Other;

        // URL del archivo
        public string FileUrl { get; set; } = string.Empty;

        

        // Relación
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
