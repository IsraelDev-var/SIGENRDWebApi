import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";

// Un borrador todavía no se envió (D-4): para el personal no existe, igual que un número equivocado.
export default function ReviewNotFound() {
  return (
    <div className="grid max-w-xl gap-6">
      <PageHeader title="No encontramos esa solicitud" description="Puede que el número esté mal o que el solicitante todavía no la haya enviado." />
      <Button asChild className="justify-self-start">
        <Link href="/gestion/solicitudes">Volver a la bandeja</Link>
      </Button>
    </div>
  );
}
