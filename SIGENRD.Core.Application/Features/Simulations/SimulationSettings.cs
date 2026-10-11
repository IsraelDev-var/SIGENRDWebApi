using SIGENRD.Core.Domain.Rules.Simulation;

namespace SIGENRD.Core.Application.Features.Simulations
{
    /// <summary>
    /// Supuestos de la simulación (sección "Simulation" de la configuración). Los valores por defecto están
    /// documentados en docs/simulacion.md; la tarifa debe actualizarse cada trimestre con la resolución de la SIE.
    /// </summary>
    public class SimulationSettings
    {
        public const string SectionName = "Simulation";

        public decimal SpecificYieldKwhPerKwp { get; set; } = 1500m;
        public decimal AnnualDegradationPct { get; set; } = 0.5m;
        public int LifetimeYears { get; set; } = 25;
        public decimal CostPerKwp { get; set; } = 60000m;
        public decimal SurplusPaymentPct { get; set; } = 75m;
        public decimal ProfitablePaybackYears { get; set; } = 6m;
        public decimal ConditionalPaybackYears { get; set; } = 10m;

        /// <summary>Factor de potencia si la distribuidora de referencia no tiene uno propio.</summary>
        public decimal DefaultPowerFactor { get; set; } = 0.9m;

        /// <summary>Radio de búsqueda de transformadores de referencia (RF-23).</summary>
        public double NearbyRadiusMeters { get; set; } = 2000;

        public TariffSettings Tariff { get; set; } = new();

        public class TariffSettings
        {
            public string Source { get; set; } = SimulationAssumptions.DefaultTariff.Source;
            public decimal? AllEnergyAtLastBlockFromKwh { get; set; } = SimulationAssumptions.DefaultTariff.AllEnergyAtLastBlockFromKwh;

            /// <summary>
            /// Tramos en orden; el último sin tope (UpToKwh vacío). Vacío usa la tarifa por defecto: el enlace de la
            /// configuración agrega a la lista existente en vez de reemplazarla, así que no se precarga.
            /// </summary>
            public List<TariffBlock> Blocks { get; set; } = new();
        }

        public SimulationAssumptions ToAssumptions() => new()
        {
            SpecificYieldKwhPerKwp = SpecificYieldKwhPerKwp,
            AnnualDegradationPct = AnnualDegradationPct,
            LifetimeYears = LifetimeYears,
            CostPerKwp = CostPerKwp,
            SurplusPaymentPct = SurplusPaymentPct,
            ProfitablePaybackYears = ProfitablePaybackYears,
            ConditionalPaybackYears = ConditionalPaybackYears,
            Tariff = Tariff.Blocks.Count == 0
                ? SimulationAssumptions.DefaultTariff
                : new ResidentialTariff(Tariff.Blocks, Tariff.AllEnergyAtLastBlockFromKwh, Tariff.Source)
        };
    }
}
