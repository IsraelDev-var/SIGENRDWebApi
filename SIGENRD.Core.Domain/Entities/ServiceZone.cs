

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Define la zona o área geográfica de servicio eléctrico.
    /// </summary>
    public class ServiceZone
    {
        public int Id { get; set; }

        // Nombre de la zona o sector
        public string ZoneName { get; set; } = string.Empty;

        // Municipio donde se encuentra
        public string? Municipality { get; set; }

        // Provincia correspondiente
        public string? Province { get; set; }

        // Código postal
        public string? PostalCode { get; set; }

        // Descripción general o detalles adicionales
        public string? Description { get; set; }

        // Relaciones
        public ICollection<Transformer>? Transformers { get; set; }
    }
}
