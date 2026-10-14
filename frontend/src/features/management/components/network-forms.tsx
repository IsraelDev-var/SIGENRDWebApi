"use client";

import { Upload } from "lucide-react";
import { useActionState, useId } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { SelectField } from "@/components/shared/form-controls";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

import {
  createTransformerAction,
  importTransformersAction,
  setMaintenanceAction,
  updateCapacityAction,
  validateInstallerAction,
  type ManagementActionState,
} from "../actions";
import { errorsByLine } from "../import-errors";

const IDLE: ManagementActionState = { status: "idle" };

type Option = { value: string; label: string };

/** Capacidad vigente (RN-01): el estado y el porcentaje los recalcula la API. */
export function CapacityForm({ id, total, available }: { id: number; total: number; available: number }) {
  const [state, action, pending] = useActionState(updateCapacityAction, IDLE);
  return (
    <form action={action} noValidate className="grid gap-3">
      <input type="hidden" name="id" value={id} />
      <FormMessage status={state.status} message={state.message} />
      <div className="grid gap-3 sm:grid-cols-2">
        <FormField
          name="totalCapacityKva"
          idPrefix="capacidad"
          label="Capacidad total (kVA)"
          inputMode="decimal"
          defaultValue={state.values?.totalCapacityKva ?? total}
          errors={state.fieldErrors?.totalCapacityKva}
        />
        <FormField
          name="availableCapacityKva"
          idPrefix="capacidad"
          label="Capacidad disponible (kVA)"
          inputMode="decimal"
          defaultValue={state.values?.availableCapacityKva ?? available}
          errors={state.fieldErrors?.availableCapacityKva}
        />
      </div>
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Guardando…" : "Guardar capacidad"}
      </Button>
    </form>
  );
}

export function MaintenanceForm({ id, inMaintenance }: { id: number; inMaintenance: boolean }) {
  const [state, action, pending] = useActionState(setMaintenanceAction, IDLE);
  return (
    <form action={action} className="grid gap-2">
      <input type="hidden" name="id" value={id} />
      <input type="hidden" name="inMaintenance" value={inMaintenance ? "false" : "true"} />
      <FormMessage status={state.status} message={state.message} />
      <p className="text-sm text-muted-foreground">
        {inMaintenance
          ? "Al sacarlo de mantenimiento, el estado se recalcula con su capacidad y vuelve a aceptar solicitudes si la tiene."
          : "En mantenimiento no acepta solicitudes nuevas y el mapa no calcula su disponibilidad."}
      </p>
      <Button type="submit" variant="outline" disabled={pending} className="justify-self-start">
        {inMaintenance ? "Sacar de mantenimiento" : "Poner en mantenimiento"}
      </Button>
    </form>
  );
}

export function CreateTransformerForm({ zones, distributors }: { zones: Option[]; distributors: Option[] }) {
  const [state, action, pending] = useActionState(createTransformerAction, IDLE);
  const e = state.fieldErrors ?? {};
  const v = state.values ?? {};
  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      {state.errors?.length ? <p className="text-sm text-destructive">{state.errors.join(" ")}</p> : null}
      <FormField name="code" idPrefix="nuevo" label="Código" required errors={e.code} defaultValue={v.code} placeholder="TR-SD-01234" />
      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField id="nuevo-distributorId" name="distributorId" label="Distribuidora" options={distributors} errors={e.distributorId} defaultValue={v.distributorId} />
        <SelectField id="nuevo-serviceZoneId" name="serviceZoneId" label="Zona" options={zones} errors={e.serviceZoneId} defaultValue={v.serviceZoneId} />
        <FormField name="totalCapacityKva" idPrefix="nuevo" label="Capacidad total (kVA)" inputMode="decimal" required errors={e.totalCapacityKva} defaultValue={v.totalCapacityKva} />
        <FormField name="availableCapacityKva" idPrefix="nuevo" label="Capacidad disponible (kVA)" inputMode="decimal" required errors={e.availableCapacityKva} defaultValue={v.availableCapacityKva} />
        <FormField name="latitude" idPrefix="nuevo" label="Latitud" inputMode="decimal" required errors={e.latitude} defaultValue={v.latitude} />
        <FormField name="longitude" idPrefix="nuevo" label="Longitud" inputMode="decimal" required errors={e.longitude} defaultValue={v.longitude} />
      </div>
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Creando…" : "Crear transformador"}
      </Button>
    </form>
  );
}

/** Importación CSV (todo o nada). Los errores de la API se agrupan por línea del archivo. */
export function ImportForm({ distributors }: { distributors: Option[] }) {
  const [state, action, pending] = useActionState(importTransformersAction, IDLE);
  const fileId = useId();
  const rows = errorsByLine(state.errors ?? []);

  return (
    <div className="grid gap-4">
      <form action={action} noValidate className="grid gap-4">
        <FormMessage status={state.status} message={state.message} />
        <div className="grid gap-4 sm:grid-cols-2">
          <SelectField id="importar-distribuidora" name="distributorId" label="Distribuidora" options={distributors} />
          <div className="grid gap-2">
            <Label htmlFor={fileId}>Archivo CSV (máximo 5 MB)</Label>
            <input
              id={fileId}
              name="file"
              type="file"
              accept=".csv,text/csv"
              required
              className="text-sm file:mr-3 file:rounded-md file:border file:bg-muted file:px-3 file:py-1 file:text-sm"
            />
          </div>
        </div>
        <Button type="submit" disabled={pending} className="justify-self-start">
          <Upload aria-hidden="true" /> {pending ? "Importando…" : "Importar"}
        </Button>
      </form>

      {rows.length > 0 && (
        <section aria-labelledby="errores-importacion" className="grid gap-2">
          <h2 id="errores-importacion" className="font-semibold">
            Errores del archivo ({state.errors!.length})
          </h2>
          <div className="rounded-lg border bg-card">
            <Table label="Errores de la importación">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Línea</TableHead>
                  <TableHead scope="col">Problema</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((row, i) => (
                  <TableRow key={i}>
                    <TableCell className="font-mono tabular-nums">{row.line ?? "Archivo"}</TableCell>
                    <TableCell className="whitespace-normal">{row.message}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </section>
      )}
    </div>
  );
}

export function ValidateInstallerForm({ id, name }: { id: number; name: string }) {
  const [state, action, pending] = useActionState(validateInstallerAction, IDLE);
  return (
    <form action={action} className="grid justify-items-end gap-1">
      <input type="hidden" name="id" value={id} />
      <Button type="submit" size="sm" disabled={pending || state.status === "success"}>
        {state.status === "success" ? "Validada" : "Validar"}
        <span className="sr-only"> {name}</span>
      </Button>
      {state.status === "error" && (
        <p role="alert" className="text-sm text-destructive">
          {state.message}
        </p>
      )}
    </form>
  );
}
