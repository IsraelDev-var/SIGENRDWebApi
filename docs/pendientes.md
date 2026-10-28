# Pendientes

Lo que falta para terminar EnergyMap, actualizado el 2026-10-27. Al cerrar un punto, se borra de aquí y queda en el historial de git. Cada punto lleva la recomendación acordada para resolverlo.

## Desarrollo

| Pendiente | Requisito | Recomendación |
|---|---|---|
| Persistir las llaves de Data Protection | Operación | En producción, `PersistKeysToAzureBlobStorage` + `ProtectKeysWithAzureKeyVault` (los dos servicios ya existen). Sin esto, al reiniciar o con varias instancias dejan de servir los enlaces de confirmación y de restablecimiento pendientes |
| Validar al arrancar `App:EmailConfirmationUrl` y `App:PasswordResetUrl` | F2 | Definirlas como variables del App Service hacia `/confirmar-correo` y `/restablecer` del frontend, y que la API no arranque en producción si faltan (evita correos con enlaces a localhost) |
| Tokens de refresco | Seguridad | No hacerlos en la Fase I: con el BFF, la cookie httpOnly y `/sesion-expirada`, los 60 minutos de sesión son seguros. Si se piden, refresh token rotativo en la base, de un solo uso, revocado al cerrar sesión o desactivar la cuenta |
| Búsqueda por dirección en el mapa | RF-04, D-3 | Nominatim desde el BFF, con caché y respetando 1 petición/s; detrás de una interfaz para cambiar luego a MapTiler o Azure Maps si crece el tráfico |
| Proveedor del mapa base en producción | D-2 | MapTiler (plan gratuito) para la entrega; a largo plazo, PMTiles propios en Blob (sin dependencia externa, pero hay que generar y actualizar las teselas). En desarrollo se usa CARTO |
| Limpiar borradores abandonados | D-4 | `BackgroundService` diario, igual al despachador del outbox: descarta los borradores con 30 días sin cambios (valor configurable) y avisa al dueño 7 días antes |
| Cédulas con formatos distintos para el mismo titular | B-11 | Columna `DocumentNumberNormalized` (solo dígitos) con el índice único sobre ella, llenada por la migración; revisar antes si ya hay duplicados |
| Vincular a una cuenta el cliente que registró una instaladora | B-11 | `POST /customers/{id}/link-account` para Distribuidora y Admin, con pantalla en gestión; solo si la cédula del perfil coincide con la de la cuenta, y queda en la auditoría |
| Repetir las pruebas de carga en Azure | Fase 7 | Los mismos scripts de k6 de [carga.md](carga.md) contra un ambiente de prueba con la base en su plan real; lo que importa es el p95 < 2 s con la red real |
| Frontend (Next.js) | Todas las pantallas | F0 a F8 fusionadas; F9 (cierre) terminada en la rama `f9-cierre`, PR #21 abierto hacia master |
| Desplegar el frontend (F9-01) | Despliegue | `output: "standalone"`, Dockerfile de varias etapas con usuario sin privilegios, job en `cd.yml` hacia App Service y tabla de variables `ENERGYMAP_*` en [despliegue.md](despliegue.md) |
| Reducir el JavaScript inicial (F9-03) | Rendimiento | Hoy 282 KB comprimidos en `/ingresar` (meta 170 KB). Analizar los fragmentos y cargar de forma diferida lo que no usan todas las páginas; Lighthouse CI con presupuesto por ruta |
| Dos avisos en la CLI de shadcn (zod 3 y `deepmerge`) | Seguridad | Solo desarrollo y sin versión corregida de `deepmerge`; revisar `pnpm audit` al actualizar shadcn. Producción sin vulnerabilidades (2026-10-27, Next 16.4.2) |
| Prueba con lector de pantalla (NVDA) y proyecto de Playwright con WebKit (F9-10) | Accesibilidad | Pedido en la propuesta de la F9; hoy solo axe, Lighthouse y Chromium |
| Actualizar RN-12 en el documento de análisis con el estado Borrador | D-4 | Solo documentación: Borrador → Recibida (enviar) y Borrador → Cancelada (descartar) en el análisis v1.2 |

## Pospuesto por decisión (esperan a Edesur)

| Pendiente | Requisito | Recomendación |
|---|---|---|
| Plazo de 20 días de la Carta de Evaluación y escalamiento | FUN-02 | No programarlo todavía. Preguntar a Edesur: ¿días hábiles o naturales?, ¿a quién se escala? |
| Retiro automático a los 30 días sin corrección | FUN-02 | Preguntar qué pasa con la reserva de capacidad al retirar la solicitud |
| Etapas finales: acuerdo firmado, póliza, prueba de aceptación, "Completada" | FUN-02 | Pedir el formato del acuerdo y de la póliza, y quién registra la prueba de aceptación |

## Decisiones pendientes

### Datos de Edesur sin API JSON

**Regla base:** EnergyMap nunca consulta a Edesur en cada petición de usuario. Nuestra base de datos responde siempre y Edesur solo la alimenta cada cierto tiempo: capacidad nominal al cambiar o una vez al mes, carga medida una vez al día y tarifa BTS-1 cada trimestre. Las reservas de capacidad son de EnergyMap y ninguna importación las toca. La disponibilidad se calcula siempre en EnergyMap: capacidad − carga − reservas activas.

**Lo que ya existe:** importación por CSV (`POST /api/v1/Transformers/import`, máximo 5 MB, pantalla `/gestion/transformadores/importar`) y el alta y edición manual por la Distribuidora.

**Mejoras recomendadas a la importación:**

- Guardar el hash del archivo: un archivo repetido no se procesa otra vez.
- Registrar cada carga en un `ImportBatch` (autor, fecha, hash, filas correctas y con error).
- Mostrar en el mapa "Datos actualizados al <fecha>"; si una carga falla, se siguen sirviendo los últimos datos buenos y se avisa al administrador.
- Si Edesur entrega otro formato (Excel, XML o SOAP): un adaptador más detrás de una interfaz `IDistributorDataSource`, sin tocar el resto.
- Si Edesur ofrece una API más adelante: preguntar con `If-Modified-Since` o `ETag` antes de descargar, con reintentos con espera y *circuit breaker* (`Microsoft.Extensions.Http.Resilience`).
- No extraer los datos de su web (scraping): se rompe con cada cambio de la página, puede violar sus términos de uso y la capacidad no suele publicarse.

**Preguntas para Edesur:** qué formato pueden exportar (CSV, Excel, XML o una vista de su base de datos), cada cuánto, si pueden dejar los archivos de forma automática (SFTP o Blob) y qué identificador único usan para cada transformador (sin él no se pueden comparar los datos sin crear duplicados).

## A confirmar con Edesur o la SIE

Todos ya son configurables. Recomendación: reunir estas preguntas en un solo documento para Edesur y la SIE, y poner un recordatorio trimestral para la tarifa BTS-1.

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

| Acción | Prioridad | Recomendación |
|---|---|---|
| Revocar la contraseña de aplicación de Gmail que quedó en el historial público del repositorio | **Crítica** | Revocarla en la cuenta de Google y crear una nueva solo en User Secrets o Key Vault. No hace falta reescribir el historial: ya pudo copiarse, lo único que la invalida es revocarla |
| Confirmar en Azure qué IP agrega App Service a `X-Forwarded-For` delante del frontend | Antes de desplegar | Comprobarlo con una petición real en el ambiente de prueba. App Service solo: `ENERGYMAP_TRUSTED_PROXIES=1`; con Front Door delante: 2. De esto depende el límite de peticiones por IP |
| Crear y configurar Azure: base con PostGIS y `pg_trgm`, Storage, Key Vault, variables de entorno | Antes de desplegar | PostgreSQL Flexible Server con las extensiones habilitadas en `azure.extensions` (si no, fallan las migraciones). Ideal como Bicep; a mano siguiendo [despliegue.md](despliegue.md) basta para la entrega |
| Programar el respaldo diario y hacer un simulacro de restauración en el servidor real | Antes de producción | Flexible Server ya respalda a diario: subir la retención a 14–35 días y restaurar una vez en otro servidor para comprobarlo |
| Configurar las alertas de [observabilidad.md](observabilidad.md) | Antes de producción | Empezar con tres: `/health` caído, errores 5xx > 1 % y CPU de la base > 80 % |
| Activar Microsoft Defender for Storage | Recomendada | Solo en la cuenta de documentos, con escaneo al subir |
