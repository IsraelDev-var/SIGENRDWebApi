import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";

// La API responde 404 igual si la solicitud no existe o si es de otra persona (RN-22): el mensaje tampoco distingue.
export default function RequestNotFound() {
  return (
    <div className="grid max-w-xl gap-6">
      <PageHeader title="No encontramos esa solicitud" description="Puede que el enlace esté incompleto o que la solicitud no sea de tu cuenta." />
      <Button asChild className="justify-self-start">
        <Link href="/portal/solicitudes">Ver mis solicitudes</Link>
      </Button>
    </div>
  );
}
