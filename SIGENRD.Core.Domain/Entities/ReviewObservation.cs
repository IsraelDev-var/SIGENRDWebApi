namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Observaciones o comentarios técnicos/documentales en el proceso de revisión de una solicitud.
    /// </summary>
    public class ReviewObservation
    {
        public int Id { get; set; }

        // Id de la solicitud relacionada
        public int ConnectionRequestId { get; set; }

        // Comentario del revisor
        public string Comment { get; set; } = string.Empty;

        // Tipo de observación (Técnica / Documental)
        public string Type { get; set; } = "Technical";

        // Usuario que generó la observación
        public string? CreatedByUser { get; set; }

        // Fecha del registro
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relación
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
