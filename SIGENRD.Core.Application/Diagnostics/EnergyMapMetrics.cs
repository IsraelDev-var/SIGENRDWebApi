using System.Diagnostics.Metrics;

namespace SIGENRD.Core.Application.Diagnostics
{
    /// <summary>
    /// Métricas propias de EnergyMap (OpenTelemetry las exporta junto a las de ASP.NET Core).
    /// Pensadas para alertas: picos de fallos de inicio de sesión y correos que no salen.
    /// </summary>
    public static class EnergyMapMetrics
    {
        public const string MeterName = "EnergyMap";

        private static readonly Meter Meter = new(MeterName);

        /// <summary>Inicios de sesión por resultado (etiqueta result: success, failed, locked_out).</summary>
        public static readonly Counter<long> Logins =
            Meter.CreateCounter<long>("energymap.auth.logins", description: "Inicios de sesión por resultado");

        /// <summary>Correos procesados por la bandeja de salida (etiqueta result: sent, retry, failed).</summary>
        public static readonly Counter<long> Emails =
            Meter.CreateCounter<long>("energymap.email.deliveries", description: "Intentos de envío de correo por resultado");

        public static void LoginSucceeded() => Logins.Add(1, new KeyValuePair<string, object?>("result", "success"));
        public static void LoginFailed() => Logins.Add(1, new KeyValuePair<string, object?>("result", "failed"));
        public static void LoginLockedOut() => Logins.Add(1, new KeyValuePair<string, object?>("result", "locked_out"));

        public static void EmailSent() => Emails.Add(1, new KeyValuePair<string, object?>("result", "sent"));
        public static void EmailRetry() => Emails.Add(1, new KeyValuePair<string, object?>("result", "retry"));
        public static void EmailFailed() => Emails.Add(1, new KeyValuePair<string, object?>("result", "failed"));
    }
}
