// Copia el worker de MapLibre a public/ para servirlo desde nuestro propio origen.
// mapcn lo descargaba de unpkg.com en tiempo de ejecución: código de un tercero fuera de nuestro control
// y bloqueado por la CSP. La ruta lleva la versión para que la caché del navegador no sirva uno viejo.
import { copyFileSync, mkdirSync, readFileSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const packageDir = dirname(require.resolve("maplibre-gl/package.json"));
const { version } = JSON.parse(readFileSync(join(packageDir, "package.json"), "utf8"));

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const targetDir = join(root, "public", "vendor", "maplibre-gl", version);

mkdirSync(targetDir, { recursive: true });
copyFileSync(join(packageDir, "dist", "maplibre-gl-worker.mjs"), join(targetDir, "maplibre-gl-worker.mjs"));
console.log(`maplibre-gl ${version}: worker copiado a public/vendor/maplibre-gl/${version}/`);
