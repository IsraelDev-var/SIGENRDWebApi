

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una empresa distribuidora (ej: Edesur, Edenorte, etc.)
    /// </summary>
    public class Distributor
    {
        public int Id { get; set; }

        // Nombre de la distribuidora
        public string Name { get; set; } = string.Empty;

        // Siglas o abreviatura (Ej: EDESUR)
        public string Acronym { get; set; } = string.Empty;

        // Correo de contacto
        public string? ContactEmail { get; set; }

        // Teléfono de contacto
        public string? ContactPhone { get; set; }

        // Indica si la distribuidora está activa
        public bool IsActive { get; set; } = true;

        // Relaciones
        public ICollection<Transformer>? Transformers { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }
}
