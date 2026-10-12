# Pendientes

Lo que falta para terminar EnergyMap, actualizado el 2026-10-11. Al cerrar un punto, se borra de aquí y queda en el historial de git.

## Desarrollo

| Pendiente | Requisito | Notas |
|---|---|---|
| Agrupamiento (clustering) del mapa en el servidor | RNF-23 | La vista del país envía hasta 2.000 puntos; agruparlos reduce carga y tamaño |
| Repetir las pruebas de carga en Azure | Fase 7 | Las de [carga.md](carga.md) son locales; falta medir con la red, la base y el almacenamiento reales |
| Frontend (Next.js) | Todas las pantallas | Sin empezar. La API ya tiene CORS por configuración para el origen del frontend |
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
