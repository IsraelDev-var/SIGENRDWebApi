"use client";

import Link from "next/link";
import { useEffect } from "react";

import { Button } from "@/components/ui/button";

import { ErrorState } from "./page-states";

type RouteErrorProps = {
  error: Error & { digest?: string };
  /** `retry` de error.tsx: vuelve a pedir los datos al servidor y a renderizar la zona. */
  retry: () => void;
  /** Inicio de la zona, para salir del error sin depender de que la API vuelva. */
  homeHref: string;
  homeLabel: string;
};

/**
 * Contenido de los error.tsx de cada zona (F9-02). En producción Next no envía el mensaje de un error del servidor, solo
 * el digest: se muestra como código para buscarlo en los logs, nunca el detalle.
 */
export function RouteError({ error, retry, homeHref, homeLabel }: RouteErrorProps) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  // Si la página falló, generateMetadata suele haber fallado con ella: sin esto el documento queda sin título.
  return (
    <>
      <title>Error · EnergyMap</title>
      <ErrorState
        title="No pudimos cargar esta página"
        description={
          <>
            <p>El servicio no respondió como esperábamos. Tus datos no se perdieron; intenta de nuevo en unos segundos.</p>
            {error.digest && <p className="mt-2 font-mono text-xs">Código de referencia: {error.digest}</p>}
          </>
        }
        action={
          <div className="flex flex-wrap justify-center gap-2">
            <Button type="button" onClick={() => retry()}>
              Reintentar
            </Button>
            <Button asChild variant="outline">
              <Link href={homeHref}>{homeLabel}</Link>
            </Button>
          </div>
        }
      />
    </>
  );
}
