using System.Globalization;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules.Simulation
{
    /// <summary>Transformador de referencia para la simulación (RF-23): solo informativo, no reserva capacidad.</summary>
    public record NearbyTransformer(string Code, double DistanceMeters, TransformerStatus Status, decimal EffectiveAvailableKva);

    /// <summary>Potencia de los equipos registrados (RF-24): paneles en corriente continua, inversores en alterna.</summary>
    public record EquipmentSummary(decimal PanelsKw, decimal InvertersKw);

    /// <summary>Recomendaciones automáticas según el resultado (RF-25). Reglas simples y explicables.</summary>
    public static class SimulationRecommendations
    {
        private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

        /// <summary>Por encima de esta cobertura, lo que sobra se cobra a menos de lo que se ahorra consumiéndolo.</summary>
        public const decimal OversizedCoveragePct = 110m;

        /// <summary>Por debajo de esta cobertura hay margen para crecer.</summary>
        public const decimal UndersizedCoveragePct = 60m;

        /// <summary>Relación paneles/inversor razonable.</summary>
        public const decimal MaxDcAcRatio = 1.35m;
        public const decimal MinDcAcRatio = 0.8m;

        public static IReadOnlyList<string> For(
            SimulationInput input,
            SimulationResult result,
            SimulationAssumptions assumptions,
            NearbyTransformer? transformer,
            EquipmentSummary? equipment,
            decimal powerFactor)
        {
            var list = new List<string>();
            var yieldPerKw = assumptions.SpecificYieldKwhPerKwp;

            // Tamaño del sistema frente al consumo.
            if (result.CoveragePct > OversizedCoveragePct)
            {
                var suggestedKw = Math.Round(result.AnnualConsumptionKwh / yieldPerKw, 1);
                var price = assumptions.Tariff.FirstBlockPrice * assumptions.SurplusPaymentPct / 100m;
                list.Add($"El sistema produce el {N(result.CoveragePct, 0)} % de tu consumo. El excedente anual se paga a " +
                         $"RD${N(price)} por kWh, menos de lo que ahorras consumiéndolo: con unos {N(suggestedKw, 1)} kWp cubrirías tu consumo " +
                         "con una inversión menor.");
            }
            else if (result.CoveragePct < UndersizedCoveragePct && result.Viability != ViabilityStatus.NotProfitable)
            {
                var suggestedKw = Math.Round(result.AnnualConsumptionKwh * 0.9m / yieldPerKw, 1);
                list.Add($"El sistema cubre el {N(result.CoveragePct, 0)} % de tu consumo y es rentable: podrías ampliarlo a " +
                         $"unos {N(suggestedKw, 1)} kWp para cubrir cerca del 90 %.");
            }

            // Tarifa: en BTS-1 todo el consumo se cobra al precio más alto desde 701 kWh.
            var threshold = assumptions.Tariff.AllEnergyAtLastBlockFromKwh;
            if (input.FlatPricePerKwh is null && threshold.HasValue)
            {
                var monthlyNet = input.MonthlyConsumptionKwh - result.AnnualProductionKwh / 12m;
                if (monthlyNet >= threshold.Value && monthlyNet < threshold.Value + 100m)
                {
                    var extraKw = Math.Ceiling((monthlyNet - threshold.Value + 1m) * 12m / yieldPerKw * 10m) / 10m;
                    list.Add($"Con paneles seguirías consumiendo de la red unos {N(monthlyNet, 0)} kWh al mes, apenas por encima de " +
                             $"{N(threshold.Value, 0)} kWh, donde toda la energía se cobra al precio más alto. Agregar unos " +
                             $"{N(extraKw, 1)} kWp bajaría tu factura de tramo.");
                }
            }

            // Rentabilidad.
            if (result.Viability == ViabilityStatus.NotProfitable)
            {
                list.Add(result.AnnualSavings <= 0
                    ? "Con estos datos el sistema no genera ahorro. Revisa el consumo y la potencia indicados."
                    : $"El retorno de {N(result.PaybackYears ?? 0, 1)} años supera los {N(assumptions.ConditionalPaybackYears, 0)} años " +
                      "considerados rentables. Compara cotizaciones: un costo por kWp menor acorta el retorno.");
            }
            else if (result.Viability == ViabilityStatus.ConditionallyProfitable)
            {
                list.Add($"El retorno de {N(result.PaybackYears ?? 0, 1)} años es aceptable pero largo. Conviene confirmar el costo " +
                         "real con una instaladora validada antes de decidir.");
            }

            // Red cercana (RF-23).
            var requiredKva = powerFactor > 0 ? input.InstalledCapacityKw / powerFactor : input.InstalledCapacityKw;
            if (transformer is null)
            {
                list.Add("No hay transformadores registrados cerca de la ubicación. La solicitud formal requerirá que la " +
                         "distribuidora indique el punto de conexión.");
            }
            else if (transformer.Status is TransformerStatus.Saturated or TransformerStatus.Maintenance)
            {
                list.Add($"El transformador más cercano ({transformer.Code}) no admite nuevas conexiones por ahora " +
                         $"({Describe(transformer.Status)}). La solicitud podría demorar o requerir otro punto de conexión.");
            }
            else if (transformer.EffectiveAvailableKva < requiredKva)
            {
                list.Add($"El transformador más cercano ({transformer.Code}) tiene {N(transformer.EffectiveAvailableKva, 1)} kVA libres y " +
                         $"el sistema necesitaría unos {N(requiredKva, 1)} kVA. Un sistema más pequeño o la evaluación de la distribuidora lo resolverían.");
            }
            else if (transformer.Status == TransformerStatus.Critical)
            {
                list.Add($"El transformador más cercano ({transformer.Code}) tiene poca capacidad libre: si te interesa, " +
                         "presenta la solicitud pronto. La simulación no reserva capacidad.");
            }

            // Equipos (RF-24).
            if (equipment is { PanelsKw: > 0, InvertersKw: > 0 })
            {
                var ratio = equipment.PanelsKw / equipment.InvertersKw;
                if (ratio > MaxDcAcRatio)
                    list.Add($"Los paneles suman {N(equipment.PanelsKw, 1)} kWp y los inversores {N(equipment.InvertersKw, 1)} kW: " +
                             "el inversor es pequeño y recortará producción en las horas de más sol.");
                else if (ratio < MinDcAcRatio)
                    list.Add($"Los inversores ({N(equipment.InvertersKw, 1)} kW) superan con holgura a los paneles " +
                             $"({N(equipment.PanelsKw, 1)} kWp): un inversor menor costaría menos sin perder producción.");
            }

            if (result.Viability == ViabilityStatus.Profitable && list.Count == 0)
                list.Add("El sistema está bien dimensionado para tu consumo. El siguiente paso es la solicitud formal de medición neta.");

            return list;
        }

        private static string Describe(TransformerStatus status) => status switch
        {
            TransformerStatus.Saturated => "saturado",
            TransformerStatus.Maintenance => "en mantenimiento",
            _ => status.ToString()
        };

        private static string N(decimal value, int decimals = 2) => value.ToString("N" + decimals, Es);
    }
}
