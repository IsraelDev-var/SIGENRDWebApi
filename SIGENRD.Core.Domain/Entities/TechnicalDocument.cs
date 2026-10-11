

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

        // Tipo MIME validado y tamaño (RN-18)
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }

        // Clave del archivo en el almacén (Blob Storage o disco). No es pública: se descarga por la API (RN-22).
        public string FileUrl { get; set; } = string.Empty;

        

        // Relación
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
