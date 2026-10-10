using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules
{
    /// <summary>
    /// Reglas RN-01 y RN-02: porcentaje de disponibilidad y estado del transformador.
    /// Es lógica pura, sin dependencias, para poder probarla sin base de datos.
    /// </summary>
    public static class TransformerAvailability
    {
        public const decimal DefaultAvailableThresholdPct = 70m;
        public const decimal DefaultConditionedThresholdPct = 30m;

        /// <summary>
        /// (Disponible - Reservas) / Total x 100, con dos decimales.
        /// Si el transformador está sobrecomprometido el resultado es negativo.
        /// Un transformador sin capacidad total se considera sin disponibilidad.
        /// </summary>
        public static decimal Percentage(decimal totalKva, decimal availableKva, decimal reservedKva = 0m)
        {
            if (totalKva <= 0m) return 0m;

            return Math.Round((availableKva - reservedKva) / totalKva * 100m, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Umbrales continuos y sin huecos. No decide Maintenance, que es administrativo.
        /// </summary>
        public static TransformerStatus Classify(
            decimal percentage,
            decimal availableThresholdPct = DefaultAvailableThresholdPct,
            decimal conditionedThresholdPct = DefaultConditionedThresholdPct)
        {
            if (percentage >= availableThresholdPct) return TransformerStatus.Available;
            if (percentage >= conditionedThresholdPct) return TransformerStatus.Conditioned;
            if (percentage > 0m) return TransformerStatus.Critical;

            return TransformerStatus.Saturated;
        }

        /// <summary>
        /// RN-03: Saturated y Maintenance no admiten solicitudes nuevas.
        /// </summary>
        public static bool AcceptsNewRequests(TransformerStatus status) =>
            status is TransformerStatus.Available or TransformerStatus.Conditioned or TransformerStatus.Critical;
    }
}
