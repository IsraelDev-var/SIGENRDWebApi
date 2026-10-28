"use client";

import { RouteError } from "@/components/shared/route-error";

// Queda dentro del layout público: el menú sigue visible aunque la página falle.
export default function PublicError(props: { error: Error & { digest?: string }; retry: () => void }) {
  return (
    <div className="mx-auto w-full max-w-xl px-4 py-6">
      <RouteError {...props} homeHref="/" homeLabel="Ir al mapa" />
    </div>
  );
}
