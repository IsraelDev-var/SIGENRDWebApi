import type { Metadata } from "next";

import { PageHeader } from "@/components/shared/page-header";
import { ImportForm, listDistributors, listZones } from "@/features/management";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Importar transformadores" };

const COLUMNS = [
  { name: "code", meaning: "Código único del transformador" },
  { name: "serviceZoneId", meaning: "Número de la zona (ver la tabla de zonas)" },
  { name: "totalCapacityKva", meaning: "Capacidad total en kVA" },
  { name: "availableCapacityKva", meaning: "Capacidad disponible en kVA (no mayor que la total)" },
  { name: "latitude", meaning: "Latitud con punto decimal, por ejemplo 18.4861" },
  { name: "longitude", meaning: "Longitud con punto decimal, por ejemplo -69.9312" },
];

/** Carga masiva desde la plantilla CSV (todo o nada): si una fila falla no se importa ninguna. */
export default async function ImportTransformersPage() {
  await requireAction("gestion", "/gestion/transformadores/importar", "manageCatalogs");
  const [zones, distributors] = await Promise.all([listZones(), listDistributors()]);

  return (
    <div className="grid max-w-3xl gap-6">
      <PageHeader
        title="Importar transformadores"
        description="Sube un CSV con la plantilla. Si alguna fila tiene un error, no se importa ninguna y verás cada problema con su línea."
      />

      <ImportForm distributors={distributors.map((d) => ({ value: String(d.id), label: d.name }))} />

      <section aria-labelledby="plantilla" className="grid gap-3">
        <h2 id="plantilla" className="text-lg font-semibold">
          Plantilla
        </h2>
        <p className="text-sm text-muted-foreground">
          Primera fila con estos nombres de columna, separadas por coma. Hasta 10,000 filas y 5 MB.{" "}
          <a href="/plantilla-transformadores.csv" download className="text-link underline underline-offset-4 hover:no-underline">
            Descargar la plantilla
          </a>
          .
        </p>
        <ul className="grid gap-1 text-sm">
          {COLUMNS.map((c) => (
            <li key={c.name}>
              <code className="font-mono">{c.name}</code>: {c.meaning}
            </li>
          ))}
        </ul>
        <details className="rounded-lg border p-4 text-sm">
          <summary className="cursor-pointer font-medium">Zonas ({zones.length})</summary>
          <ul className="mt-3 grid gap-1 sm:grid-cols-2">
            {zones.map((z) => (
              <li key={z.id}>
                <span className="font-mono">{z.id}</span> · {z.zoneName}
              </li>
            ))}
          </ul>
        </details>
      </section>
    </div>
  );
}
