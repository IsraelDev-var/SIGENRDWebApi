"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { NuqsAdapter } from "nuqs/adapters/next/app";
import { useState, type ReactNode } from "react";
import { z } from "zod";

// La Content Security Policy no permite eval: zod lo intentaba al cargar para elegir su modo rápido (y quedaba un aviso
// de la CSP en cada página). Así usa directamente el modo sin eval.
z.config({ jitless: true });

/** Proveedores del navegador: caché de consultas del mapa (TanStack Query) y estado en la URL (nuqs). */
export function Providers({ children }: { children: ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          // Los datos del mapa cambian poco y la API los cachea 30 s: no se repiden al volver a la pestaña.
          queries: { refetchOnWindowFocus: false, retry: 1 },
        },
      }),
  );

  return (
    <QueryClientProvider client={client}>
      <NuqsAdapter>{children}</NuqsAdapter>
    </QueryClientProvider>
  );
}
