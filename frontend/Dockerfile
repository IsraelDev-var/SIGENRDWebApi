# syntax=docker/dockerfile:1
# Imagen de producción del frontend de EnergyMap (Next.js, modo standalone). F9-01.
#   docker build -t energymap-web frontend
# Los estilos del mapa son NEXT_PUBLIC_*: Next los escribe en el JavaScript al compilar, así que van como --build-arg.
# Todo lo demás (URL de la API, llave pública del JWT, clave del proxy) llega por variables de entorno al arrancar.

# Igual que la API (sdk:9.0, aspnet:9.0): versión mayor fija y parches de seguridad al reconstruir.
ARG NODE_VERSION=24

# ---- dependencias ----
FROM node:${NODE_VERSION}-alpine AS deps
WORKDIR /app
RUN corepack enable
# Primero solo los manifiestos: la instalación queda en caché mientras no cambien las dependencias.
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

# ---- compilación ----
FROM node:${NODE_VERSION}-alpine AS build
WORKDIR /app
RUN corepack enable
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_MAP_STYLE_LIGHT=""
ARG NEXT_PUBLIC_MAP_STYLE_DARK=""
ENV NEXT_PUBLIC_MAP_STYLE_LIGHT=${NEXT_PUBLIC_MAP_STYLE_LIGHT} \
    NEXT_PUBLIC_MAP_STYLE_DARK=${NEXT_PUBLIC_MAP_STYLE_DARK} \
    ENERGYMAP_STANDALONE=true \
    NEXT_TELEMETRY_DISABLED=1
RUN pnpm build

# ---- ejecución ----
FROM node:${NODE_VERSION}-alpine AS final
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=8080 \
    HOSTNAME=0.0.0.0

# El código queda de root y solo de lectura para el proceso; únicamente la caché de Next es escribible.
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
RUN mkdir -p .next/cache && chown node:node .next/cache

# Usuario sin privilegios (viene en la imagen base) y puerto sin privilegios.
USER node
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/api/salud > /dev/null || exit 1

CMD ["node", "server.js"]
