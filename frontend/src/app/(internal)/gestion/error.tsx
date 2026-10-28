"use client";

import { RouteError } from "@/components/shared/route-error";

// Queda dentro del layout de gestión: el menú y la sesión siguen visibles aunque la API falle.
export default function InternalError(props: { error: Error & { digest?: string }; retry: () => void }) {
  return <RouteError {...props} homeHref="/gestion" homeLabel="Ir al inicio de gestión" />;
}
