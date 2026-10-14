"use client";

import { useActionState, useState } from "react";

import { FormMessage } from "@/components/shared/form-field";
import { SelectField, TextareaField } from "@/components/shared/form-controls";
import { Button } from "@/components/ui/button";

import { reviewAction, type ManagementActionState } from "../actions";

const IDLE: ManagementActionState = { status: "idle" };

const OBSERVATION_TYPES = [
  { value: "Technical", label: "Técnica" },
  { value: "Documental", label: "Documental" },
  { value: "Administrative", label: "Administrativa" },
];

type Props = {
  requestId: number;
  status: string;
  /** Ingeniero, distribuidora y administrador: pasar a revisión y observar. */
  canReview: boolean;
  /** Solo distribuidora y administrador aprueban o rechazan (matriz 3.2). */
  canDecide: boolean;
};

type Open = "observe" | "reject" | "approve" | null;

/** Transiciones del personal según RN-12. Los botones siguen la tabla de estados; la API decide igual (CA-07). */
export function ReviewActions({ requestId, status, canReview, canDecide }: Props) {
  const [state, action, pending] = useActionState(reviewAction, IDLE);
  const [open, setOpen] = useState<Open>(null);

  const hidden = (transition: string) => (
    <>
      <input type="hidden" name="id" value={requestId} />
      <input type="hidden" name="transition" value={transition} />
    </>
  );

  const waiting =
    status === "Observed" || status === "InCorrection"
      ? "Espera a que el solicitante corrija y reenvíe la solicitud."
      : status === "Approved" || status === "Rejected" || status === "Cancelled"
        ? "La solicitud está cerrada: no admite más cambios (RN-07)."
        : null;

  return (
    <section aria-labelledby="revision" className="grid gap-3 rounded-lg border bg-card p-4">
      <h2 id="revision" className="font-semibold">
        Revisión
      </h2>
      <FormMessage status={state.status} message={state.message} />
      {state.errors && state.errors.length > 0 && (
        <ul className="list-disc pl-5 text-sm text-destructive">
          {state.errors.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}

      {status === "Received" && canReview && (
        <form action={action} className="grid gap-2">
          {hidden("start-review")}
          <p className="text-sm text-muted-foreground">Al pasar a revisión se reserva la capacidad pedida en el transformador (RN-15).</p>
          <Button type="submit" disabled={pending} className="justify-self-start">
            Pasar a revisión
          </Button>
        </form>
      )}

      {status === "UnderReview" && (
        <div className="grid gap-3">
          {!canDecide && <p className="text-sm text-muted-foreground">La aprobación o el rechazo los decide la distribuidora.</p>}
          <div className="flex flex-wrap gap-2">
            {canReview && (
              <Button type="button" variant={open === "observe" ? "secondary" : "outline"} onClick={() => setOpen(open === "observe" ? null : "observe")}>
                Emitir observaciones
              </Button>
            )}
            {canDecide && (
              <>
                <Button type="button" variant={open === "approve" ? "secondary" : "default"} onClick={() => setOpen(open === "approve" ? null : "approve")}>
                  Aprobar
                </Button>
                <Button type="button" variant="destructive" onClick={() => setOpen(open === "reject" ? null : "reject")}>
                  Rechazar
                </Button>
              </>
            )}
          </div>

          {open === "observe" && (
            <form action={action} noValidate className="grid gap-3 rounded-md border p-3">
              {hidden("observe")}
              <SelectField
                id="tipo-observacion"
                name="observationType"
                label="Tipo de observación"
                options={OBSERVATION_TYPES}
                defaultValue={state.values?.observationType ?? "Technical"}
              />
              <TextareaField
                id="comentario-observacion"
                name="comment"
                label="Qué debe corregir el solicitante"
                required
                maxLength={500}
                errors={state.fieldErrors?.comment}
                defaultValue={state.values?.transition === "observe" ? state.values.comment : undefined}
                description="Lo verá el solicitante en su expediente y en el correo."
              />
              <Button type="submit" disabled={pending} className="justify-self-start">
                Enviar observaciones
              </Button>
            </form>
          )}

          {open === "approve" && (
            <form action={action} className="grid gap-3 rounded-md border p-3">
              {hidden("approve")}
              <p className="text-sm">
                ¿Aprobar la solicitud? Se genera el certificado y la capacidad reservada pasa a estar comprometida. No se puede deshacer (RN-07).
              </p>
              <div className="flex flex-wrap gap-2">
                <Button type="submit" disabled={pending}>
                  Sí, aprobar
                </Button>
                <Button type="button" variant="outline" onClick={() => setOpen(null)}>
                  No, volver
                </Button>
              </div>
            </form>
          )}

          {open === "reject" && (
            <form action={action} noValidate className="grid gap-3 rounded-md border border-destructive/40 p-3">
              {hidden("reject")}
              <TextareaField
                id="motivo-rechazo"
                name="comment"
                label="Motivo del rechazo"
                required
                maxLength={500}
                errors={state.fieldErrors?.comment}
                defaultValue={state.values?.transition === "reject" ? state.values.comment : undefined}
                description="Lo verá el solicitante. La capacidad reservada se libera."
              />
              <Button type="submit" variant="destructive" disabled={pending} className="justify-self-start">
                Rechazar la solicitud
              </Button>
            </form>
          )}
        </div>
      )}

      {status === "Approved" && (
        <Button asChild variant="outline" className="justify-self-start">
          <a href={`/gestion/solicitudes/${requestId}/certificado`} download>
            Descargar el certificado
          </a>
        </Button>
      )}

      {waiting && <p className="text-sm text-muted-foreground">{waiting}</p>}
    </section>
  );
}
