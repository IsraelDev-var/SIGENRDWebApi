

using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa una empresa distribuidora (ej: Edesur, Edenorte, etc.)
    /// </summary>
    public class Distributor : AuditableEntity
    {
        public int Id { get; set; }

        // Nombre de la distribuidora
        public string Name { get; set; } = string.Empty;

        // Enum que define el tipo de distribuidora
        public DistributorType Type { get; set; }

        // Correo de contacto
        public string? ContactEmail { get; set; }

        // Teléfono de contacto
        public string? ContactPhone { get; set; }

       

        // Umbrales de disponibilidad configurables (RN-02) y factor de potencia para kW a kVA (RN-01)
        public decimal AvailableThresholdPct { get; set; } = 70m;
        public decimal ConditionedThresholdPct { get; set; } = 30m;
        public decimal DefaultPowerFactor { get; set; } = 0.9m; // valor provisional, por confirmar con Edesur

        // Relaciones
        public ICollection<Transformer>? Transformers { get; set; }
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }
}
