"use client";

import { Clock } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";

const WARN_MS = 5 * 60 * 1000;

/**
 * Aviso de que la sesión (JWT de 60 minutos, sin renovación) está por terminar, para que nadie pierda un formulario a
 * medias. Al vencer, ofrece volver a ingresar a la misma página.
 */
export function SessionExpiryNotice({ expiresAt }: { expiresAt: number }) {
  const pathname = usePathname();
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 30_000);
    return () => window.clearInterval(id);
  }, []);

  const left = expiresAt - now;
  if (left > WARN_MS) return null;

  const next = encodeURIComponent(pathname);
  const minutes = Math.max(1, Math.ceil(left / 60_000));

  return (
    <div role="status" className="flex flex-wrap items-center gap-2 border-b bg-tone-warning-soft px-4 py-2 text-sm text-tone-warning-text">
      <Clock aria-hidden="true" className="size-4 shrink-0" />
      {left > 0 ? (
        <span>
          Tu sesión termina en {minutes === 1 ? "1 minuto" : `${minutes} minutos`}. Guarda lo que estés haciendo.
        </span>
      ) : (
        <span>Tu sesión terminó.</span>
      )}
      <Link href={`/sesion-expirada?next=${next}`} className="font-medium underline underline-offset-4 hover:no-underline">
        Volver a ingresar
      </Link>
    </div>
  );
}
