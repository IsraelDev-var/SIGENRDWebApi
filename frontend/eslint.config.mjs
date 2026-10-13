import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";
import boundaries from "eslint-plugin-boundaries";

// Límites de importación entre capas (sección 5 del documento de arquitectura).
// "feature" en una lista quiere decir: solo su entrada pública (src/features/<nombre>/index.ts).
const layers = {
  app: ["app", "feature", "components", "server", "lib", "config", "hooks"],
  feature: ["feature", "components", "server", "lib", "config", "hooks"],
  components: ["components", "lib", "config", "hooks"],
  hooks: ["hooks", "lib", "config"],
  server: ["server", "lib", "config"],
  lib: ["lib", "config"],
  config: ["config", "lib"],
};

const target = (type) =>
  type === "feature"
    ? { element: { type: "feature" }, file: { categories: "entry" } }
    : { element: { type } };

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  {
    files: ["src/**/*.{ts,tsx}"],
    plugins: { boundaries },
    settings: {
      "import/resolver": { typescript: { alwaysTryTypes: true } },
      "boundaries/elements": [
        { type: "app", pattern: "src/app", partialMatch: false },
        { type: "feature", pattern: "src/features/*", partialMatch: false, capture: ["feature"] },
        { type: "components", pattern: "src/components", partialMatch: false },
        { type: "hooks", pattern: "src/hooks", partialMatch: false },
        { type: "server", pattern: "src/server", partialMatch: false },
        { type: "lib", pattern: "src/lib", partialMatch: false },
        { type: "config", pattern: "src/config", partialMatch: false },
      ],
      "boundaries/files": [{ pattern: "src/features/*/index.ts", category: "entry" }],
    },
    rules: {
      "boundaries/dependencies": [
        "error",
        {
          default: "disallow",
          message: "{{from.element.type}} no puede importar {{to.element.type}} (límites en frontend/README.md).",
          policies: [
            // Paquetes externos y módulos de Node: permitidos.
            { allow: { to: { module: { origin: "external" } } } },
            { allow: { to: { module: { origin: "core" } } } },
            ...Object.entries(layers).flatMap(([from, types]) =>
              types.map((type) => ({ from: { element: { type: from } }, allow: { to: target(type) } })),
            ),
            // Dentro de una misma feature, cualquier archivo.
            {
              from: { element: { type: "feature" } },
              allow: {
                to: { element: { type: "feature", captured: { feature: "{{from.element.captured.feature}}" } } },
              },
            },
          ],
        },
      ],
    },
  },
  {
    // Código de mapcn (shadcn add @mapcn/map): usa a propósito el patrón "latest ref" y estados de carga en efectos.
    // No se reescribe para poder compararlo con el original; React Compiler no está activo en el proyecto.
    files: ["src/components/ui/map.tsx"],
    rules: {
      "react-hooks/refs": "off",
      "react-hooks/set-state-in-effect": "off",
    },
  },
  // Reportes de Playwright (JS minificado) y archivos generados.
  globalIgnores([
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
    "public/vendor/**",
    "src/lib/api/schema.d.ts",
    "playwright-report/**",
    "test-results/**",
    "blob-report/**",
  ]),
]);

export default eslintConfig;
