"use client";

import { RouteError } from "@/components/shared/route-error";

export default function AuthError(props: { error: Error & { digest?: string }; retry: () => void }) {
  return <RouteError {...props} homeHref="/" homeLabel="Ir al mapa" />;
}
