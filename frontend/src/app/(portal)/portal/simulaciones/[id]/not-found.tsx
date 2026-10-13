import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";

// La API responde 404 igual si no existe o si es de otra persona: el mensaje tampoco distingue.
export default function SimulationNotFound() {
  return (
    <div className="grid max-w-xl gap-6">
      <PageHeader title="No encontramos esa simulación" description="Puede que el enlace esté incompleto o que la simulación no sea de tu cuenta." />
      <Button asChild className="justify-self-start">
        <Link href="/portal/simulaciones">Ver mis simulaciones</Link>
      </Button>
    </div>
  );
}
