#!/usr/bin/env bash
# Respaldo lógico de la base de EnergyMap (negocio + Identity) con pg_dump, en formato custom comprimido.
#
#   DATABASE_URL="postgresql://usuario:clave@host:5432/base?sslmode=require" ./scripts/backup-db.sh [carpeta]
#
# Variables:
#   DATABASE_URL           conexión de solo lectura basta (obligatoria)
#   BACKUP_RETENTION_DAYS  días que se conservan los respaldos de la carpeta (por defecto 30)
#
# Requiere pg_dump/pg_restore de la misma versión mayor que el servidor o superior.
set -euo pipefail

: "${DATABASE_URL:?Define DATABASE_URL}"
dir="${1:-./backups}"
retention="${BACKUP_RETENTION_DAYS:-30}"

mkdir -p "$dir"
name="energymap-$(date -u +%Y%m%dT%H%M%SZ).dump"

# Se escribe a un .partial y se renombra solo si el respaldo se puede leer completo.
pg_dump --format=custom --no-owner --no-privileges --file="$dir/$name.partial" "$DATABASE_URL"
pg_restore --list "$dir/$name.partial" > /dev/null
mv "$dir/$name.partial" "$dir/$name"
(cd "$dir" && sha256sum "$name" > "$name.sha256")

find "$dir" -maxdepth 1 -type f -name 'energymap-*.dump*' -mtime +"$retention" -delete

echo "Respaldo listo: $dir/$name ($(du -h "$dir/$name" | cut -f1))"
