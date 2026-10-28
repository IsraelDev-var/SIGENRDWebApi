"use client";

import { RouteError } from "@/components/shared/route-error";

import "./globals.css";

// Solo aparece si falla el layout raíz. Reemplaza el documento completo, por eso lleva su propio <html> y no tiene las
// fuentes de next/font (usa la pila del sistema).
export default function GlobalError(props: { error: Error & { digest?: string }; retry: () => void }) {
  return (
    <html lang="es-DO" className="h-full antialiased">
      <body className="flex min-h-full flex-col">
        <main id="contenido" className="mx-auto w-full max-w-xl flex-1 px-4 py-16">
          <RouteError {...props} homeHref="/" homeLabel="Ir al mapa" />
        </main>
      </body>
    </html>
  );
}
