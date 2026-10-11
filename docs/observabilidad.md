# Observabilidad

Tres señales, todas apagadas o mínimas por defecto y activadas por configuración.

## Logs

- **Desarrollo:** texto por consola y archivo diario en `logs/`.
- **Fuera de desarrollo:** una línea JSON por evento en la salida estándar (formato compacto de Serilog: `@t` fecha, `@m` mensaje, `@l` nivel, `@x` excepción). Cada línea lleva `@tr` (TraceId) y `@sp` (SpanId): con ellos se cruzan los logs con las trazas de la misma petición.
- La plataforma recoge la salida estándar: en Azure Container Apps o App Service van a Log Analytics sin configurar nada en la API.
- Las sondas de `/health` no se registran salvo que fallen.

## Trazas y métricas (OpenTelemetry)

Se activan solo si existe `OTEL_EXPORTER_OTLP_ENDPOINT`. Sin ella no se instala nada.

| Variable | Ejemplo | Para qué |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | Destino OTLP (Collector, Grafana Cloud, Honeycomb, Azure Monitor vía Collector…) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` (por defecto) o `http/protobuf` | Protocolo del destino |
| `OTEL_EXPORTER_OTLP_HEADERS` | `api-key=...` | Credencial del destino, si la pide. Es un secreto |
| `OTEL_SERVICE_NAME` | `energymap-api` (por defecto) | Nombre del servicio en las trazas |

**Trazas:** cada petición HTTP (salvo `/health`), las llamadas HTTP salientes y cada consulta a PostgreSQL.

**Métricas:**

| Métrica | Origen |
|---|---|
| `http.server.request.duration` | Latencia y código de cada endpoint (ASP.NET Core) |
| `process.runtime.dotnet.*` | Memoria, GC, hilos (runtime) |
| `energymap.auth.logins{result=success\|failed\|locked_out}` | Inicios de sesión por resultado |
| `energymap.email.deliveries{result=sent\|retry\|failed}` | Intentos de la bandeja de salida de correo |

Verificado el 2026-10-10 con un OpenTelemetry Collector 0.110.0 local: llegaron trazas (28 spans de arranque y tres peticiones) y 34 métricas cada 2 s.

## Alertas recomendadas

Se configuran en la herramienta que reciba las señales (Azure Monitor, Grafana…). Umbrales de partida, a ajustar con el tráfico real:

| Alerta | Condición | Severidad |
|---|---|---|
| API no lista | `/health` distinto de 200 durante 2 minutos | Crítica |
| Errores del servidor | Respuestas 5xx > 1 % de las peticiones en 5 minutos | Alta |
| Latencia | p95 de `http.server.request.duration` > 1 s durante 10 minutos | Media |
| Posible fuerza bruta | `energymap.auth.logins{result=failed}` > 50 en 10 minutos, o cualquier `locked_out` de una cuenta de personal | Alta |
| Correo que no sale | `energymap.email.deliveries{result=failed}` > 0, o `retry` sostenido 30 minutos | Alta |
| Respaldo | El respaldo diario no terminó (ver [respaldo.md](respaldo.md)) | Crítica |

Para revisar a mano la bandeja de correo:

```sql
SELECT "Id", "To", "Subject", "Attempts", "LastError", "NextAttemptAt", "FailedAt"
FROM "OutboxEmails" WHERE "SentAt" IS NULL ORDER BY "CreatedAt";
```

Un correo con `FailedAt` se puede reintentar poniendo `"FailedAt" = NULL, "Attempts" = 0, "NextAttemptAt" = now()`.
