import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

import { Brand, SkipLink } from "./brand";
import { MobileNav } from "./mobile-nav";
import { NavLinks, type NavItem } from "./nav-links";

type ShellProps = {
  nav: NavItem[];
  /** Zona derecha de la barra: usuario, notificaciones, ingresar. */
  actions?: ReactNode;
  children: ReactNode;
  /** Ruta activa fija (catálogo). */
  activeHref?: string;
  className?: string;
};

/** Zona pública: cabecera ligera; el contenido (el mapa) ocupa el resto. */
export function PublicShell({ nav, actions, children, activeHref, className }: ShellProps) {
  return (
    <div className={cn("flex min-h-full flex-1 flex-col", className)}>
      <SkipLink />
      <header className="flex h-14 items-center gap-3 border-b bg-card px-4">
        <MobileNav items={nav} />
        <Brand />
        <nav aria-label="Principal" className="hidden md:block">
          <NavLinks items={nav} activeHref={activeHref} />
        </nav>
        <div className="ml-auto flex items-center gap-2">{actions}</div>
      </header>
      <main id="contenido" className="flex flex-1 flex-col">
        {children}
      </main>
      <footer className="border-t bg-card px-4 py-2 text-xs text-muted-foreground">
        Datos referenciales cargados por la distribuidora (Edesur, Fase I). No sustituyen la evaluación técnica de una solicitud.
      </footer>
    </div>
  );
}

/** Portal de Cliente e Instaladora: barra superior y contenido centrado. */
export function PortalShell({ nav, actions, children, activeHref, className }: ShellProps) {
  return (
    <div className={cn("flex min-h-full flex-1 flex-col", className)}>
      <SkipLink />
      <header className="border-b bg-card">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-3 px-4">
          <MobileNav items={nav} title="Mi portal" />
          <Brand />
          <nav aria-label="Mi portal" className="hidden md:block">
            <NavLinks items={nav} activeHref={activeHref} />
          </nav>
          <div className="ml-auto flex items-center gap-2">{actions}</div>
        </div>
      </header>
      <main id="contenido" className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        {children}
      </main>
    </div>
  );
}

/** Gestión (Ingeniero, Distribuidora, Admin): barra lateral y mayor densidad para bandejas y tablas. */
export function InternalShell({ nav, actions, children, activeHref, className }: ShellProps) {
  return (
    <div className={cn("flex min-h-full flex-1", className)}>
      <SkipLink />
      <aside className="hidden w-60 shrink-0 flex-col gap-4 border-r bg-sidebar p-3 text-sidebar-foreground md:flex">
        <Brand className="px-2 py-1" />
        <nav aria-label="Gestión">
          <NavLinks items={nav} orientation="vertical" activeHref={activeHref} />
        </nav>
      </aside>
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center gap-3 border-b bg-card px-4">
          <MobileNav items={nav} title="Gestión" />
          <span className="md:hidden">
            <Brand />
          </span>
          <div className="ml-auto flex items-center gap-2">{actions}</div>
        </header>
        <main id="contenido" className="flex-1 px-4 py-6 md:px-6">
          {children}
        </main>
      </div>
    </div>
  );
}

export type { NavItem };
