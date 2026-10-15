"use client";

import { useQuery } from "@tanstack/react-query";
import { Bell } from "lucide-react";
import Link from "next/link";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

async function fetchCount(): Promise<number> {
  const response = await fetch("/api/notificaciones/conteo", { cache: "no-store" });
  if (!response.ok) return 0;
  const body = (await response.json()) as { count?: unknown };
  return typeof body.count === "number" ? body.count : 0;
}

/**
 * Campana con las notificaciones sin leer (RF-32). El conteo se pide al servidor de Next (que lleva el token) cada
 * minuto y al volver a la pestaña: el layout no se vuelve a renderizar al navegar, así que no puede traerlo él.
 */
export function NotificationBell({ href }: { href: string }) {
  const { data: count = 0 } = useQuery({
    queryKey: ["notificaciones", "conteo"],
    queryFn: fetchCount,
    refetchInterval: 60_000,
    refetchOnWindowFocus: true,
  });

  const label = count === 0 ? "Notificaciones" : `Notificaciones: ${count > 99 ? "más de 99" : count} sin leer`;

  return (
    <Button asChild variant="ghost" size="icon" className="relative">
      <Link href={href} aria-label={label} title={label}>
        <Bell aria-hidden="true" />
        {count > 0 && (
          <span
            aria-hidden="true"
            className={cn(
              "absolute -top-0.5 -right-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-destructive px-1",
              "text-[10px] leading-none font-semibold text-white tabular-nums",
            )}
          >
            {count > 99 ? "99+" : count}
          </span>
        )}
      </Link>
    </Button>
  );
}
