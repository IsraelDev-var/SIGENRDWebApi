#!/usr/bin/env bash
# Restaura un respaldo de backup-db.sh en una base de datos.
#
#   TARGET_DATABASE_URL="postgresql://usuario:clave@host:5432/base_nueva?sslmode=require" \
#   CONFIRM_RESTORE=si ./scripts/restore-db.sh backups/energymap-AAAAMMDDTHHMMSSZ.dump
#
# Recomendado: restaurar siempre en una base NUEVA y vacía, comprobarla y después apuntar la API a ella.
# La base destino necesita PostGIS disponible (en Azure, habilitado en azure.extensions).
set -euo pipefail

file="${1:?Indica el archivo .dump a restaurar}"
: "${TARGET_DATABASE_URL:?Define TARGET_DATABASE_URL}"

# Si el respaldo trae suma de verificación, se comprueba antes de tocar nada.
if [ -f "$file.sha256" ]; then
  (cd "$(dirname "$file")" && sha256sum --check --quiet "$(basename "$file").sha256")
fi

if [ "${CONFIRM_RESTORE:-}" != "si" ]; then
  echo "La restauración reemplaza los objetos que ya existan en la base destino."
  echo "Repite el comando con CONFIRM_RESTORE=si para continuar."
  exit 1
fi

pg_restore --clean --if-exists --no-owner --no-privileges --exit-on-error \
  --dbname="$TARGET_DATABASE_URL" "$file"

echo "Restauración completa en la base destino."
