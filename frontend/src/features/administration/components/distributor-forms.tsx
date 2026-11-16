"use client";

import { useActionState, useState } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { SelectField } from "@/components/shared/form-controls";
import { Button } from "@/components/ui/button";

import { saveDistributorAction, setDistributorActiveAction, type AdminActionState } from "../actions";

const IDLE: AdminActionState = { status: "idle" };

const TYPE_OPTIONS = [
  { value: "EDESUR", label: "Edesur" },
  { value: "EDENORTE", label: "Edenorte" },
  { value: "EDEESTE", label: "Edeeste" },
  { value: "OTRA", label: "Otra (sistema aislado o concesión)" },
];

export type DistributorValues = {
  id?: number;
  name: string;
  type: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  availableThresholdPct: number;
  conditionedThresholdPct: number;
  defaultPowerFactor: number;
};

const NEW: DistributorValues = { name: "", type: "EDENORTE", availableThresholdPct: 70, conditionedThresholdPct: 30, defaultPowerFactor: 0.9 };

/** Alta o edición de una EDE: datos de contacto, umbrales de disponibilidad (RN-02) y factor de potencia (RN-01). */
export function DistributorForm({ distributor = NEW }: { distributor?: DistributorValues }) {
  const [state, action, pending] = useActionState(saveDistributorAction, IDLE);
  const e = state.fieldErrors ?? {};
  // Tras un error se muestra lo enviado; si no, lo guardado.
  const v = state.status === "error" && state.values ? state.values : undefined;
  const editing = distributor.id !== undefined;

  return (
    <form action={action} noValidate className="grid gap-4">
      {editing && <input type="hidden" name="id" value={distributor.id} />}
      <FormMessage status={state.status} message={state.message} />
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField name="name" idPrefix="ede" label="Nombre" required autoComplete="off" defaultValue={v?.name ?? distributor.name} errors={e.name} />
        <SelectField id="ede-type" name="type" label="Tipo" options={TYPE_OPTIONS} defaultValue={v?.type ?? distributor.type} errors={e.type} />
        <FormField
          name="contactEmail"
          idPrefix="ede"
          label="Correo de contacto"
          type="email"
          autoComplete="off"
          defaultValue={v?.contactEmail ?? distributor.contactEmail ?? ""}
          errors={e.contactEmail}
        />
        <FormField
          name="contactPhone"
          idPrefix="ede"
          label="Teléfono de contacto"
          type="tel"
          autoComplete="off"
          defaultValue={v?.contactPhone ?? distributor.contactPhone ?? ""}
          errors={e.contactPhone}
        />
      </div>
      <fieldset className="grid gap-4 rounded-lg border p-4">
        <legend className="px-1 text-sm font-medium">Disponibilidad de los transformadores</legend>
        <div className="grid gap-4 sm:grid-cols-3">
          <FormField
            name="availableThresholdPct"
            idPrefix="ede"
            label="Disponible desde (%)"
            inputMode="decimal"
            defaultValue={v?.availableThresholdPct ?? distributor.availableThresholdPct}
            errors={e.availableThresholdPct}
          />
          <FormField
            name="conditionedThresholdPct"
            idPrefix="ede"
            label="Condicionada desde (%)"
            inputMode="decimal"
            defaultValue={v?.conditionedThresholdPct ?? distributor.conditionedThresholdPct}
            errors={e.conditionedThresholdPct}
          />
          <FormField
            name="defaultPowerFactor"
            idPrefix="ede"
            label="Factor de potencia"
            inputMode="decimal"
            description="Para pasar kW a kVA."
            defaultValue={v?.defaultPowerFactor ?? distributor.defaultPowerFactor}
            errors={e.defaultPowerFactor}
          />
        </div>
        <p className="text-sm text-muted-foreground">
          Por debajo de condicionada es crítica, y sin capacidad, saturada.
          {editing && " Si cambias los umbrales, el estado de sus transformadores se recalcula."}
        </p>
      </fieldset>
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Guardando…" : editing ? "Guardar cambios" : "Crear distribuidora"}
      </Button>
    </form>
  );
}

/** Desactivar pide confirmación; reactivar no, porque no quita nada. */
export function DistributorStatusForm({ id, name, active }: { id: number; name: string; active: boolean }) {
  const [state, action, pending] = useActionState(setDistributorActiveAction, IDLE);
  const [confirming, setConfirming] = useState(false);

  return (
    <div className="grid justify-items-start gap-2">
      {active && !confirming ? (
        <Button type="button" variant="outline" className="text-destructive" onClick={() => setConfirming(true)}>
          Desactivar distribuidora
        </Button>
      ) : (
        <form action={action} className="flex flex-wrap items-center gap-2">
          <input type="hidden" name="id" value={id} />
          <input type="hidden" name="active" value={active ? "false" : "true"} />
          {active && (
            <span className="text-sm">
              ¿Desactivar {name}? Deja de ofrecerse en el mapa y no admite transformadores ni personal nuevos; lo que ya tiene se conserva.
            </span>
          )}
          <Button type="submit" variant={active ? "destructive" : "outline"} disabled={pending}>
            {active ? "Sí, desactivar" : "Reactivar distribuidora"}
          </Button>
          {active && (
            <Button type="button" variant="ghost" onClick={() => setConfirming(false)}>
              No
            </Button>
          )}
        </form>
      )}
      {state.status !== "idle" && (
        <p role={state.status === "error" ? "alert" : "status"} className={state.status === "error" ? "text-sm text-destructive" : "text-sm"}>
          {state.message}
        </p>
      )}
    </div>
  );
}
