using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Simulación técnico-económica de medición neta. Es informativa: no es una solicitud
    /// y no reserva capacidad del transformador.
    /// </summary>
    public class NetMeteringSimulation : AuditableEntity
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        // Transformador y distribuidora de referencia (opcionales)
        public int? DistributorId { get; set; }
        public int? TransformerId { get; set; }

        public decimal InstalledCapacityKw { get; set; }
        public decimal AnnualProductionKwh { get; set; }
        public decimal AnnualSavings { get; set; }
        public decimal InvestmentCost { get; set; }
        public decimal PaybackYears { get; set; }
        public decimal Roi { get; set; }

        public ViabilityStatus ViabilityStatus { get; set; }

        public Distributor? Distributor { get; set; }
        public Transformer? Transformer { get; set; }
        public ICollection<SimulationGenerator>? Generators { get; set; }
    }
}
