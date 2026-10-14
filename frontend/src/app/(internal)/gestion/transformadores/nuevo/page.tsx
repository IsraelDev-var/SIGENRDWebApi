import type { Metadata } from "next";

import { PageHeader } from "@/components/shared/page-header";
import { CreateTransformerForm, listDistributors, listZones } from "@/features/management";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Nuevo transformador" };

export default async function NewTransformerPage() {
  await requireAction("gestion", "/gestion/transformadores/nuevo", "manageCatalogs");
  const [zones, distributors] = await Promise.all([listZones(), listDistributors()]);

  return (
    <div className="grid max-w-2xl gap-6">
      <PageHeader title="Nuevo transformador" description="El estado inicial lo calcula el sistema con la capacidad que indiques." />
      <CreateTransformerForm
        zones={zones.map((z) => ({ value: String(z.id), label: z.zoneName }))}
        distributors={distributors.map((d) => ({ value: String(d.id), label: d.name }))}
      />
    </div>
  );
}
