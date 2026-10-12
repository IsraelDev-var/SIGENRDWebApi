"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

import type { NavItem } from "@/config/navigation";
import { cn } from "@/lib/utils";

import { NAV_ICONS } from "./nav-icons";

export type { NavItem };

type Props = {
  items: NavItem[];
  orientation?: "horizontal" | "vertical";
  /** Ruta activa fija, para el catálogo; normalmente se toma de la URL. */
  activeHref?: string;
  className?: string;
};

function matches(pathname: string, href: string): boolean {
  return href === "/" ? pathname === "/" : pathname === href || pathname.startsWith(`${href}/`);
}

/**
 * El enlace activo es el que más se acerca a la ruta: en /gestion/solicitudes lo es "Solicitudes",
 * no "Panel" (/gestion), aunque las dos rutas coincidan por prefijo.
 */
export function findActiveHref(items: NavItem[], pathname: string): string | undefined {
  return items
    .map((item) => item.href)
    .filter((href) => matches(pathname, href))
    .sort((a, b) => b.length - a.length)[0];
}

export function NavLinks({ items, orientation = "horizontal", activeHref, className }: Props) {
  const pathname = usePathname();
  const active = findActiveHref(items, activeHref ?? pathname);

  return (
    <ul className={cn("flex gap-1", orientation === "vertical" ? "flex-col" : "flex-wrap items-center", className)}>
      {items.map(({ href, label, icon }) => {
        const isCurrent = href === active;
        const Icon = icon ? NAV_ICONS[icon] : null;
        return (
          <li key={href}>
            <Link
              href={href}
              aria-current={isCurrent ? "page" : undefined}
              className={cn(
                "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                "hover:bg-secondary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
                isCurrent ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:text-foreground",
              )}
            >
              {Icon && <Icon aria-hidden="true" className="size-4 shrink-0" />}
              {label}
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
