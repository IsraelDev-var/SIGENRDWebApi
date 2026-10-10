using SIGENRD.Core.Domain.Base;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Equipo (paneles e inversor) usado en una simulación.
    /// </summary>
    public class SimulationGenerator : AuditableEntity
    {
        public int Id { get; set; }

        public int NetMeteringSimulationId { get; set; }

        public GenerationSystemType GenerationSystemType { get; set; }

        public string? PanelModel { get; set; }
        public string? InverterModel { get; set; }

        public decimal RatedPowerKw { get; set; }
        public int Quantity { get; set; } = 1;

        public NetMeteringSimulation? NetMeteringSimulation { get; set; }
    }
}
