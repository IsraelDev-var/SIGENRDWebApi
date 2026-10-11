# Respaldo y restauración de la base de datos

La base guarda solicitudes, historial de estados, auditoría y cuentas: perderla es perder el expediente de cada trámite. Hay dos capas de respaldo independientes.

## 1. Respaldo del proveedor (principal)

En **Azure Database for PostgreSQL – Flexible Server**:

| Ajuste | Valor recomendado |
|---|---|
| Retención de respaldos | 35 días (máximo) |
| Respaldo con redundancia geográfica | Activado (se elige al crear el servidor; no se puede cambiar después) |
| Restauración a un punto en el tiempo (PITR) | Incluida: a cualquier segundo dentro de la retención |

PITR siempre restaura en un **servidor nuevo**. Después se cambian `ConnectionStrings__Postgres` y `ConnectionStrings__IdentityServerDB` para apuntar a él.

## 2. Respaldo lógico propio (complementario)

Sirve para guardar copias fuera del proveedor, para retenciones mayores a 35 días y para restaurar en cualquier PostgreSQL con PostGIS.

```bash
DATABASE_URL="postgresql://usuario:clave@host:5432/base?sslmode=require" ./scripts/backup-db.sh /ruta/respaldos
```

- Formato custom de `pg_dump`, comprimido, con suma SHA-256 al lado.
- Se escribe a un archivo temporal y solo se da por bueno si `pg_restore --list` lo puede leer completo.
- Borra los respaldos de esa carpeta con más de `BACKUP_RETENTION_DAYS` días (30 por defecto).
- Basta un usuario de **solo lectura**.

**Dónde guardarlos.** En almacenamiento privado y cifrado (por ejemplo, un contenedor de Azure Blob Storage con inmutabilidad por tiempo). **Nunca** como artefacto de GitHub Actions: el repositorio es público y los respaldos contienen datos personales.

**Cuándo.** Una vez al día, programado en la infraestructura donde se despliegue (Azure Container Apps Job, cron del servidor, etc.).

## Restauración

```bash
TARGET_DATABASE_URL="postgresql://usuario:clave@host:5432/base_nueva?sslmode=require" \
CONFIRM_RESTORE=si ./scripts/restore-db.sh /ruta/respaldos/energymap-AAAAMMDDTHHMMSSZ.dump
```

1. Crear una base **nueva y vacía** con PostGIS disponible.
2. Restaurar: el script comprueba la suma SHA-256 y se niega a correr sin `CONFIRM_RESTORE=si`.
3. Comprobar conteos y una consulta de mapa contra la base restaurada.
4. Apuntar la API a la base nueva y verificar que `/health` responde `Healthy`.

## Simulacros

Se hace un simulacro de restauración al menos una vez por trimestre y después de cada cambio grande del modelo. Se anota aquí.

| Fecha | Entorno | Datos | Resultado |
|---|---|---|---|
| 2026-10-10 | PostGIS 17-3.5 local (contenedor desechable), migraciones al día | 5.000 transformadores, 200 eventos de auditoría | Respaldo de 108 KB; restauración en base nueva en 0,6 s. Origen y restaurada **idénticas**: conteos por tabla, huella MD5 de los transformadores (código, capacidad y ubicación), consulta espacial a 2 km (265 en ambas), 60 índices, 2 triggers, 6 migraciones. Restauración sin `CONFIRM_RESTORE` rechazada; respaldo alterado rechazado por la suma SHA-256 |

Pendiente: repetir el simulacro contra el servidor real una vez desplegado, incluida una restauración PITR.
