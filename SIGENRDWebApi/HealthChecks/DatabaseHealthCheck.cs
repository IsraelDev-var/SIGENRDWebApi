using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SIGENRDWebApi.HealthChecks
{
    /// <summary>
    /// Lista para atender solo si responde la base del contexto. Sin base la API no sirve de nada,
    /// así que el balanceador debe dejar de enviarle tráfico. Se registra uno por contexto (negocio e Identity).
    /// </summary>
    public class DatabaseHealthCheck<TContext>(TContext context, TimeSpan? timeout = null) : IHealthCheck
        where TContext : DbContext
    {
        /// <summary>Holgado para el primer uso del contexto, que construye el modelo de EF.</summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
        {
            var limit = timeout ?? DefaultTimeout;

            try
            {
                // Npgsql no siempre atiende la cancelación mientras abre la conexión (servidor que acepta y no responde):
                // WaitAsync corta la espera a tiempo aunque el intento siga en segundo plano.
                var connected = await context.Database.CanConnectAsync(cancellationToken).WaitAsync(limit, cancellationToken);

                return connected
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy($"No responde la base de datos ({typeof(TContext).Name}).");
            }
            catch (TimeoutException)
            {
                return HealthCheckResult.Unhealthy($"La base de datos ({typeof(TContext).Name}) no respondió en {limit.TotalSeconds:0} s.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"Error al conectar con la base de datos ({typeof(TContext).Name}).", ex);
            }
        }
    }
}
