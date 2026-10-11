#!/usr/bin/env bash
# Pruebas de carga locales (fase 7). Levanta la imagen real de la API limitada a 2 vCPU y 2 GB, con PostGIS,
# Azurite y 10.000 transformadores / 20.000 solicitudes, y corre los escenarios de k6 indicados.
#
#   tests/load/run.sh                      # mapa (CA-12)
#   tests/load/run.sh mapa mixto documentos estres
#   KEEP=1 tests/load/run.sh mapa          # deja el entorno levantado al terminar
#
# Requiere Docker, el SDK de .NET y dotnet-ef (para generar los scripts de migración, igual que el CD).
# Los resultados quedan en tests/load/results/<escenario>.json y .txt.
set -euo pipefail

cd "$(dirname "$0")"
# Git Bash (Windows) convierte rutas como /scripts/x.js en rutas de Windows: aquí son rutas del contenedor.
export MSYS_NO_PATHCONV=1
ROOT="$(cd ../.. && pwd)"
COMPOSE=(docker compose -f docker-compose.load.yml)
SCENARIOS=("${@:-mapa}")

# Secretos solo de esta corrida: nada se guarda.
export LOAD_DB_PASSWORD="$(openssl rand -hex 16)"
export LOAD_ADMIN_PASSWORD="Carga-$(openssl rand -hex 6)-A1"
export LOAD_JWT_KEY="$(openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 2>/dev/null | openssl pkcs8 -topk8 -nocrypt -outform DER | base64 | tr -d '\n')"

cleanup() {
  if [ "${KEEP:-0}" != "1" ]; then "${COMPOSE[@]}" down -v --remove-orphans >/dev/null 2>&1 || true; fi
}
trap cleanup EXIT

echo "== Scripts de migración"
mkdir -p .generated results
(cd "$ROOT" && dotnet ef migrations script --idempotent -p SIGENRD.Infrastructure.Persistences -s SIGENRD.Infrastructure.Persistences \
  -c AppContextSIGENRD -o tests/load/.generated/1-persistence.sql >/dev/null)
(cd "$ROOT" && dotnet ef migrations script --idempotent -p SIGENRD.Infrastructure.Identity -s SIGENRD.Infrastructure.Identity \
  -c IdentityDbContextSIGENRD -o tests/load/.generated/2-identity.sql >/dev/null)

echo "== Base de datos"
"${COMPOSE[@]}" down -v --remove-orphans >/dev/null 2>&1 || true
"${COMPOSE[@]}" up -d --wait db azurite
psql_db() { "${COMPOSE[@]}" exec -T db psql -q -U postgres -d energymap -v ON_ERROR_STOP=1 "$@"; }
psql_db < .generated/1-persistence.sql >/dev/null
psql_db < .generated/2-identity.sql >/dev/null
time psql_db < seed.sql
echo "$(psql_db -At -c 'SELECT count(*) FROM "Transformers"') transformadores, $(psql_db -At -c 'SELECT count(*) FROM "ConnectionRequests"') solicitudes"

echo "== API (imagen de producción, 2 vCPU, 2 GB)"
"${COMPOSE[@]}" up -d --build api
for _ in $(seq 1 60); do
  if curl -sf http://127.0.0.1:5299/health >/dev/null; then break; fi
  sleep 2
done
curl -sf http://127.0.0.1:5299/health >/dev/null || { "${COMPOSE[@]}" logs api | tail -30; exit 1; }

for scenario in "${SCENARIOS[@]}"; do
  echo "== Escenario: $scenario"
  # Uso de CPU y memoria de la API y la base cada 5 s mientras corre el escenario.
  ( while true; do
      docker stats --no-stream --format '{{.Name}};{{.CPUPerc}};{{.MemUsage}}' | grep -E 'carga-(api|db)-1' | sed "s/^/$(date +%T);/"
      sleep 5
    done ) > "results/$scenario-recursos.csv" 2>/dev/null &
  sampler=$!

  status=0
  "${COMPOSE[@]}" run --rm k6 run --quiet --summary-export "/results/$scenario.json" "/scripts/$scenario.js" \
    > "results/$scenario.txt" 2>&1 || status=$?
  kill "$sampler" 2>/dev/null || true
  cat "results/$scenario.txt"
  for svc in api db; do
    max=$(grep "carga-$svc-1" "results/$scenario-recursos.csv" | cut -d';' -f3 | tr -d '%' | sort -n | tail -1)
    echo "CPU máxima de $svc durante $scenario: ${max:-?} % (100 % = 1 núcleo)"
  done | tee -a "results/$scenario.txt"
  # k6 sale con 99 cuando no se cumple algún umbral; cualquier otro código es un error de la corrida.
  case $status in
    0) ;;
    99) echo "(el escenario $scenario no cumplió algún umbral)" ;;
    *) echo "ERROR: k6 falló con el código $status en $scenario"; exit "$status" ;;
  esac

done
