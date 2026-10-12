import { Zap } from "lucide-react";
import Link from "next/link";

import { cn } from "@/lib/utils";

/** Nombre de la plataforma. EnergyMap es una plataforma propia: no usa la marca de Edesur. */
export function Brand({ className }: { className?: string }) {
  return (
    <Link href="/" className={cn("flex items-center gap-2 rounded-md font-semibold tracking-tight", className)}>
      <span className="flex size-7 items-center justify-center rounded-md bg-primary text-primary-foreground">
        <Zap aria-hidden="true" className="size-4" />
      </span>
      EnergyMap
    </Link>
  );
}

/** Primer elemento enfocable de cada página: salta la navegación (WCAG 2.4.1). */
export function SkipLink() {
  return (
    <a
      href="#contenido"
      className="sr-only rounded-md bg-primary px-3 py-2 text-primary-foreground focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50"
    >
      Saltar al contenido
    </a>
  );
}
