import { CircleAlert, Inbox, Lock, type LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

type StateProps = {
  title: string;
  description?: ReactNode;
  /** Acción para salir del estado: crear el primer elemento, reintentar, iniciar sesión. */
  action?: ReactNode;
  className?: string;
};

function PageState({ icon: Icon, title, description, action, className, role }: StateProps & { icon: LucideIcon; role?: "alert" | "status" }) {
  return (
    <div role={role} className={cn("flex flex-col items-center gap-3 rounded-lg border border-dashed px-6 py-10 text-center", className)}>
      <Icon aria-hidden="true" className="size-8 text-muted-foreground" />
      <div className="grid gap-1">
        <p className="font-medium">{title}</p>
        {description && <div className="max-w-prose text-sm text-muted-foreground">{description}</div>}
      </div>
      {action}
    </div>
  );
}

/** Lista o búsqueda sin resultados. Dice qué falta y cómo seguir. */
export function EmptyState(props: StateProps) {
  return <PageState icon={Inbox} role="status" {...props} />;
}

/** Error al cargar: explica qué pasó y ofrece reintentar. */
export function ErrorState(props: StateProps) {
  return <PageState icon={CircleAlert} role="alert" {...props} />;
}

/** La API respondió 403 o el rol no tiene permiso (RN-22). */
export function ForbiddenState(props: StateProps) {
  return <PageState icon={Lock} {...props} />;
}
