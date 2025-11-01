

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un documento técnico adjunto a una solicitud de conexión.
    /// </summary>
    public class TechnicalDocument
    {
        public int Id { get; set; }

        // Id de la solicitud asociada
        public int ConnectionRequestId { get; set; }

        // Nombre del archivo
        public string FileName { get; set; } = string.Empty;

        // Tipo de documento (Plano, Permiso, Ficha técnica)
        public string DocumentType { get; set; } = string.Empty;

        // URL del archivo
        public string FileUrl { get; set; } = string.Empty;

        // Fecha de carga
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Usuario que subió el documento
        public string? UploadedBy { get; set; }

        // Relación
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
