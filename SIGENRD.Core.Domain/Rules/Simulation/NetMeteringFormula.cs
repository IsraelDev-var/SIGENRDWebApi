using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules.Simulation
{
    /// <summary>Un tramo de la tarifa: precio de los kWh hasta UpToKwh (null = sin tope).</summary>
    public record TariffBlock(decimal? UpToKwh, decimal PricePerKwh);

    /// <summary>
    /// Tarifa residencial escalonada (BTS-1). Desde AllEnergyAtLastBlockFromKwh todo el consumo del mes
    /// se cobra al precio del último tramo, como en las tarifas de transición de la SIE.
    /// </summary>
    public record ResidentialTariff(IReadOnlyList<TariffBlock> Blocks, decimal? AllEnergyAtLastBlockFromKwh, string Source)
    {
        public decimal FirstBlockPrice => Blocks[0].PricePerKwh;

        /// <summary>Cargo por energía de un mes (sin cargo fijo, que se paga igual con o sin paneles).</summary>
        public decimal EnergyCharge(decimal kwh)
        {
            if (kwh <= 0) return 0;

            if (AllEnergyAtLastBlockFromKwh.HasValue && kwh >= AllEnergyAtLastBlockFromKwh.Value)
                return kwh * Blocks[^1].PricePerKwh;

            decimal charge = 0, from = 0;
            foreach (var block in Blocks)
            {
                var to = block.UpToKwh ?? decimal.MaxValue;
                if (kwh <= from) break;
                charge += (Math.Min(kwh, to) - from) * block.PricePerKwh;
                from = to;
            }
            return charge;
        }
    }

    /// <summary>
    /// Supuestos del cálculo. Todos son configurables; los valores por defecto están documentados en docs/simulacion.md
    /// y los marcados como provisionales deben confirmarse con Edesur o la SIE.
    /// </summary>
    public record SimulationAssumptions
    {
        /// <summary>kWh que produce cada kWp instalado en un año, ya con pérdidas del sistema.</summary>
        public decimal SpecificYieldKwhPerKwp { get; init; } = 1500m;

        /// <summary>Pérdida anual de producción de los paneles, en %.</summary>
        public decimal AnnualDegradationPct { get; init; } = 0.5m;

        public int LifetimeYears { get; init; } = 25;

        /// <summary>Costo instalado por kWp cuando el usuario no indica su cotización (provisional).</summary>
        public decimal CostPerKwp { get; init; } = 60000m;

        /// <summary>% del crédito acumulado a diciembre que paga la distribuidora (reglamento de medición neta).</summary>
        public decimal SurplusPaymentPct { get; init; } = 75m;

        /// <summary>Hasta estos años de retorno es Rentable; hasta ConditionalPaybackYears, Condicionalmente rentable (provisionales).</summary>
        public decimal ProfitablePaybackYears { get; init; } = 6m;
        public decimal ConditionalPaybackYears { get; init; } = 10m;

        public ResidentialTariff Tariff { get; init; } = DefaultTariff;

        /// <summary>Edesur, BTS-1 de transición, julio-septiembre de 2026 (resolución SIE-079-2026-TF).</summary>
        public static readonly ResidentialTariff DefaultTariff = new(
            new[]
            {
                new TariffBlock(200m, 6.05m),
                new TariffBlock(300m, 8.59m),
                new TariffBlock(700m, 12.89m),
                new TariffBlock(null, 13.09m)
            },
            AllEnergyAtLastBlockFromKwh: 701m,
            Source: "Edesur BTS-1, tarifa de transición jul-sep 2026 (SIE-079-2026-TF)");
    }

    public record SimulationInput(
        decimal MonthlyConsumptionKwh,
        decimal InstalledCapacityKw,
        decimal? InvestmentCost = null,
        decimal? FlatPricePerKwh = null);

    public record SimulationResult(
        decimal AnnualProductionKwh,
        decimal AnnualConsumptionKwh,
        decimal CoveragePct,
        decimal AnnualBillBefore,
        decimal AnnualBillAfter,
        decimal AnnualExportCreditKwh,
        decimal SurplusPayment,
        decimal AnnualSavings,
        decimal InvestmentCost,
        decimal? PaybackYears,
        decimal Roi,
        ViabilityStatus Viability);

    /// <summary>Fórmula de rentabilidad (Strategy): se reemplaza sin tocar el resto si la SIE cambia el cálculo.</summary>
    public interface INetMeteringFormula
    {
        string Version { get; }
        SimulationResult Calculate(SimulationInput input, SimulationAssumptions assumptions);
    }

    /// <summary>
    /// Versión 1.
    /// - Producción anual = kWp × rendimiento específico; se reparte igual en los 12 meses.
    /// - Medición neta mes a mes: lo producido cubre el consumo; el sobrante pasa como crédito en kWh al mes siguiente.
    ///   El crédito que queda a diciembre se paga al SurplusPaymentPct del precio del primer tramo BTS-1.
    /// - Ahorro anual = factura de energía sin paneles − factura con paneles + pago del crédito.
    /// - Retorno simple = inversión / ahorro anual.
    /// - ROI = (ahorro de toda la vida útil, con la degradación anual aplicada al ahorro, − inversión) / inversión × 100.
    /// </summary>
    public class NetMeteringFormulaV1 : INetMeteringFormula
    {
        public string Version => "v1";

        public SimulationResult Calculate(SimulationInput input, SimulationAssumptions a)
        {
            var monthlyProduction = input.InstalledCapacityKw * a.SpecificYieldKwhPerKwp / 12m;
            var consumption = input.MonthlyConsumptionKwh;

            decimal Charge(decimal kwh) => input.FlatPricePerKwh is { } price ? Math.Max(kwh, 0) * price : a.Tariff.EnergyCharge(kwh);

            decimal billBefore = 0, billAfter = 0, credit = 0;
            for (var month = 0; month < 12; month++)
            {
                billBefore += Charge(consumption);

                var available = monthlyProduction + credit;
                if (available >= consumption)
                {
                    credit = available - consumption;
                }
                else
                {
                    billAfter += Charge(consumption - available);
                    credit = 0;
                }
            }

            var surplusPayment = credit * a.Tariff.FirstBlockPrice * a.SurplusPaymentPct / 100m;
            var savings = billBefore - billAfter + surplusPayment;
            var investment = input.InvestmentCost ?? input.InstalledCapacityKw * a.CostPerKwp;

            decimal? payback = savings > 0 ? investment / savings : null;

            decimal lifetimeSavings = 0, factor = 1;
            for (var year = 0; year < a.LifetimeYears; year++)
            {
                lifetimeSavings += savings * factor;
                factor *= 1 - a.AnnualDegradationPct / 100m;
            }
            var roi = investment > 0 ? (lifetimeSavings - investment) / investment * 100m : 0;

            var viability = payback switch
            {
                null => ViabilityStatus.NotProfitable,
                var p when p <= a.ProfitablePaybackYears => ViabilityStatus.Profitable,
                var p when p <= a.ConditionalPaybackYears => ViabilityStatus.ConditionallyProfitable,
                _ => ViabilityStatus.NotProfitable
            };

            var annualProduction = monthlyProduction * 12m;
            var annualConsumption = consumption * 12m;

            return new SimulationResult(
                AnnualProductionKwh: Round(annualProduction),
                AnnualConsumptionKwh: Round(annualConsumption),
                CoveragePct: annualConsumption > 0 ? Round(annualProduction / annualConsumption * 100m) : 0,
                AnnualBillBefore: Round(billBefore),
                AnnualBillAfter: Round(billAfter),
                AnnualExportCreditKwh: Round(credit),
                SurplusPayment: Round(surplusPayment),
                AnnualSavings: Round(savings),
                InvestmentCost: Round(investment),
                PaybackYears: payback is null ? null : Round(payback.Value),
                Roi: Round(roi),
                Viability: viability);
        }

        private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
