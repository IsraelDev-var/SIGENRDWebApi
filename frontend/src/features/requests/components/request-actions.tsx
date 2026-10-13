"use client";

import Link from "next/link";
import { useActionState, useState } from "react";

import { FormMessage } from "@/components/shared/form-field";
import { Button } from "@/components/ui/button";

import { requestTransitionAction, type RequestActionState } from "../actions";
import { TextareaField } from "./fields";

const IDLE: RequestActionState = { status: "idle" };

type Props = {
  requestId: number;
  status: string;
  /** Todos los documentos requeridos están (RN-09): sin ellos no se envía. */
  documentsComplete: boolean;
};

/** Lo que el solicitante puede hacer según el estado (RN-12, RN-23, D-4). La API decide si la transición vale. */
export function RequestActions({ requestId, status, documentsComplete }: Props) {
  const [state, action, pending] = useActionState(requestTransitionAction, IDLE);
  const [confirming, setConfirming] = useState(false);

  const isDraft = status === "Draft";
  const canCancel = ["Draft", "Received", "Observed", "InCorrection"].includes(status);
  const primary =
    status === "Draft"
      ? { transition: "submit", label: "Enviar a Edesur", needsDocuments: true }
      : status === "Observed"
        ? { transition: "start-correction", label: "Corregir la solicitud", needsDocuments: false }
        : status === "InCorrection"
          ? { transition: "resubmit", label: "Reenviar a Edesur", needsDocuments: true }
          : null;

  // Tras la última acción posible (reenviar, cancelar) no queda nada que hacer, pero el resultado se sigue mostrando.
  if (!primary && !canCancel && status !== "Approved" && state.status === "idle") return null;

  return (
    <section aria-labelledby="acciones" className="grid gap-3 rounded-lg border bg-card p-4">
      <h2 id="acciones" className="font-semibold">
        Acciones
      </h2>
      <FormMessage status={state.status} message={state.message} />

      {primary && (
        <form action={action} className="grid gap-2">
          <input type="hidden" name="id" value={requestId} />
          <input type="hidden" name="transition" value={primary.transition} />
          {primary.needsDocuments && !documentsComplete && (
            <p id="faltan-documentos" className="text-sm text-muted-foreground">
              Sube todos los documentos requeridos para poder enviarla.
            </p>
          )}
          <Button
            type="submit"
            disabled={pending || (primary.needsDocuments && !documentsComplete)}
            aria-describedby={primary.needsDocuments && !documentsComplete ? "faltan-documentos" : undefined}
            className="justify-self-start"
          >
            {pending ? "Enviando…" : primary.label}
          </Button>
        </form>
      )}

      {isDraft && (
        <Button asChild variant="outline" className="justify-self-start">
          <Link href={`/portal/solicitudes/${requestId}/editar`}>Editar los datos</Link>
        </Button>
      )}

      {status === "Approved" && (
        <Button asChild className="justify-self-start">
          <a href={`/portal/solicitudes/${requestId}/certificado`} download>
            Descargar el certificado de aprobación
          </a>
        </Button>
      )}

      {canCancel &&
        (confirming ? (
          <form action={action} className="grid gap-3 rounded-md border border-destructive/40 p-3">
            <input type="hidden" name="id" value={requestId} />
            <input type="hidden" name="transition" value={isDraft ? "discard" : "cancel"} />
            <p className="text-sm font-medium">
              {isDraft
                ? "¿Descartar el borrador? Dejará de aparecer como pendiente y no se podrá enviar."
                : "¿Cancelar la solicitud? Edesur dejará de revisarla y no se puede deshacer (RN-23)."}
            </p>
            {!isDraft && <TextareaField id="motivo-cancelacion" name="comment" label="Motivo (opcional)" maxLength={500} />}
            <div className="flex flex-wrap gap-2">
              <Button type="submit" variant="destructive" disabled={pending}>
                {isDraft ? "Sí, descartar" : "Sí, cancelar la solicitud"}
              </Button>
              <Button type="button" variant="outline" onClick={() => setConfirming(false)}>
                No, volver
              </Button>
            </div>
          </form>
        ) : (
          <Button type="button" variant="ghost" className="justify-self-start text-destructive" onClick={() => setConfirming(true)}>
            {isDraft ? "Descartar borrador" : "Cancelar la solicitud"}
          </Button>
        ))}
    </section>
  );
}
