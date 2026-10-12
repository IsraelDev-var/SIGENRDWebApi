# Pendientes

Lo que falta para terminar EnergyMap, actualizado el 2026-10-12. Al cerrar un punto, se borra de aquí y queda en el historial de git.

## Desarrollo

| Pendiente | Requisito | Notas |
|---|---|---|
| Repetir las pruebas de carga en Azure | Fase 7 | Las de [carga.md](carga.md) son locales; falta medir con la red, la base y el almacenamiento reales |
| Frontend (Next.js) | Todas las pantallas | F0 a F3 (fundaciones, Design System, sesión y permisos, mapa público) hechos en `frontend/`; siguen F4 a F9 según la propuesta aprobada. La API ya tiene CORS por configuración para el origen del frontend |
| Configurar `App:EmailConfirmationUrl` y `App:PasswordResetUrl` de la API hacia `/confirmar-correo` y `/restablecer` del frontend | F2 | Sin ellas, el correo de confirmación sigue apuntando a la API |
| Búsqueda por dirección en el mapa | RF-04, D-3 | Funciona por código y por coordenadas; falta elegir el geocodificador |
| Proveedor del mapa base en producción | D-2 | En desarrollo se usa CARTO; definir teselas propias (PMTiles) o un proveedor con contrato |
| Editar el perfil | RF-09 | Datos del usuario, cliente e instaladora |
| Tokens de refresco | Seguridad | Hoy el JWT dura 60 minutos y luego hay que iniciar sesión otra vez |
| Persistir las llaves de Data Protection | Operación | Sin esto, al reiniciar el contenedor o con varias instancias dejan de servir los enlaces de confirmación y de restablecimiento pendientes |
| Cabeceras reenviadas (`ForwardedHeaders`) | Operación | Necesario si la API queda detrás de un proxy: el límite de peticiones es por IP |

## Pospuesto por decisión (esperan a Edesur)

| Pendiente | Requisito |
|---|---|
| Plazo de 20 días de la Carta de Evaluación y escalamiento | FUN-02 |
| Retiro automático a los 30 días sin corrección | FUN-02 |
| Etapas finales: acuerdo firmado, póliza, prueba de aceptación, "Completada" | FUN-02 |

## Decisiones pendientes

### Cómo llegan a EnergyMap los datos de Edesur si no hay API JSON

**Regla base:** EnergyMap nunca consulta a Edesur en cada petición de usuario. Nuestra base de datos responde siempre y Edesur solo la alimenta cada cierto tiempo: capacidad nominal al cambiar o una vez al mes, carga medida una vez al día y tarifa BTS-1 cada trimestre. Las reservas de capacidad son de EnergyMap y ninguna importación las toca.

| Opción | Valoración |
|---|---|
| Archivo con plantilla fija (CSV o Excel) que sube la Distribuidora (`POST /transformers/import`), o que se deja en SFTP o Blob y se procesa de noche | **Recomendada para la Fase I** |
| Ingreso manual (lo que ya existe) | Respaldo y correcciones puntuales |
| XML o SOAP, si es lo que tiene Edesur | Viable con un adaptador más |
| Extraer los datos de su web (scraping) | No recomendada: se rompe con cada cambio de la página, puede violar sus términos de uso y la capacidad no suele publicarse |

**Diseño propuesto:** una interfaz `IDistributorDataSource` con una implementación por formato. El proceso valida fila por fila y guarda los datos en una tabla de staging junto con un `ImportBatch` (autor, fecha, hash del archivo y filas correctas o con error). Luego compara con lo actual, aplica los cambios en una transacción e invalida la caché del mapa.

- Idempotencia por hash: un archivo repetido no se procesa otra vez.
- Si la carga falla, se siguen sirviendo los últimos datos buenos, se avisa al administrador y el mapa muestra "Datos actualizados al <fecha>".
- La disponibilidad se calcula siempre en EnergyMap: capacidad − carga − reservas activas.
- Si Edesur ofrece una API más adelante: preguntar con `If-Modified-Since` o `ETag` antes de descargar, con reintentos con espera y *circuit breaker* (`Microsoft.Extensions.Http.Resilience`).

**Preguntas para Edesur:** qué formato pueden exportar (CSV, Excel, XML o una vista de su base de datos), cada cuánto, si pueden dejar los archivos de forma automática y qué identificador único usan para cada transformador (sin él no se pueden comparar los datos sin crear duplicados).

## A confirmar con Edesur o la SIE

| Supuesto | Dónde se usa | Valor provisional |
|---|---|---|
| Documentos requeridos por trámite | Solicitudes (RN-09, RN-11) | Según requisitos públicos de las EDE y el reglamento CNE 2012 |
| Factor de potencia | Capacidad en kVA | 0.9 |
| Umbrales de disponibilidad | Mapa | 70 % / 30 % |
| Costo por kWp | Simulación | RD$60,000 |
| Umbrales de rentabilidad | Simulación | 6 y 10 años de retorno |
| Pago del excedente al 75 % del primer tramo | Simulación | Reglamento de medición neta, primera edición (2016) |
| Fórmula de rentabilidad frente a su hoja de cálculo | Simulación (CA-09) | Fórmula v1, ver [simulacion.md](simulacion.md) |
| Tarifa BTS-1 | Simulación | Jul-sep 2026; **actualizar cada trimestre** |

## Acciones del equipo (fuera del código)

| Acción | Prioridad |
|---|---|
| Revocar la contraseña de aplicación de Gmail que quedó en el historial público del repositorio | **Crítica** |
| Crear y configurar Azure: base con PostGIS y `pg_trgm`, Storage, Key Vault, variables de entorno | Antes de desplegar |
| Activar Microsoft Defender for Storage (escaneo de malware al subir documentos) | Recomendada |
| Programar el respaldo diario y hacer un simulacro de restauración en el servidor real | Antes de producción |
| Configurar las alertas de [observabilidad.md](observabilidad.md) | Antes de producción |
