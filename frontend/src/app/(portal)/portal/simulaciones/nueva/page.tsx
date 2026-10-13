import type { Metadata } from "next";

import { PageHeader } from "@/components/shared/page-header";
import { emptySimulationForm, locationFrom, SimulationForm } from "@/features/simulations";
import { requireZone } from "@/server/session";

export const metadata: Metadata = { title: "Nueva simulación" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

/** Simulador (RF-18). Desde el mapa llega con ?lat=&lng= del transformador elegido. */
export default async function NewSimulationPage({ searchParams }: PageProps<"/portal/simulaciones/nueva">) {
  const params = await searchParams;
  await requireZone("portal", "/portal/simulaciones/nueva");
  const location = locationFrom(one(params.lat), one(params.lng));

  return (
    <div className="grid max-w-2xl gap-6">
      <PageHeader title="Nueva simulación" description="Con tu consumo y el tamaño del sistema solar calculamos el ahorro, el retorno y si conviene." />
      <SimulationForm initialValues={emptySimulationForm(location)} />
    </div>
  );
}
