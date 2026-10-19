"use client";

import { RouteError } from "@/components/shared/route-error";

// Queda dentro del layout del portal: el menú y la sesión siguen visibles aunque la API falle.
export default function PortalError(props: { error: Error & { digest?: string }; retry: () => void }) {
  return <RouteError {...props} homeHref="/portal" homeLabel="Ir al inicio del portal" />;
}
