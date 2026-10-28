"use client";

import { useActionState, useState } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { SelectField } from "@/components/shared/form-controls";
import { Button } from "@/components/ui/button";

import { createInternalUserAction, setUserActiveAction, setUserDistributorAction, type AdminActionState } from "../actions";

const IDLE: AdminActionState = { status: "idle" };

const ROLE_OPTIONS = [
  { value: "Engineer", label: "Ingeniero (revisa y observa)" },
  { value: "Distributor", label: "Distribuidora (decide, red, reportes)" },
  { value: "Admin", label: "Administrador (todo, usuarios y auditoría)" },
];

type Option = { value: string; label: string };

/** El ingeniero y la distribuidora pertenecen a una EDE; el administrador, a ninguna. */
const BELONGS_TO_EDE = new Set(["Engineer", "Distributor"]);

const PASSWORD_HELP = "Temporal: al menos 10 caracteres, con mayúscula, minúscula y número. Compártela por un canal seguro.";

/** Alta de usuarios internos (matriz 3.2): solo el administrador. */
export function CreateUserForm({ distributors }: { distributors: Option[] }) {
  const [state, action, pending] = useActionState(createInternalUserAction, IDLE);
  const e = state.fieldErrors ?? {};
  const v = state.status === "success" ? {} : (state.values ?? {});
  const [role, setRole] = useState(v.userType ?? "Engineer");

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField name="fullName" idPrefix="usuario" label="Nombre completo" autoComplete="off" required defaultValue={v.fullName} errors={e.fullName} />
        <FormField name="email" idPrefix="usuario" label="Correo" type="email" autoComplete="off" required defaultValue={v.email} errors={e.email} />
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField
          id="usuario-userType"
          name="userType"
          label="Rol"
          options={ROLE_OPTIONS}
          value={role}
          onChange={(event) => setRole(event.target.value)}
          errors={e.userType}
        />
        {BELONGS_TO_EDE.has(role) && (
          <SelectField
            id="usuario-distributorId"
            name="distributorId"
            label="Distribuidora"
            options={[{ value: "", label: "Elige la EDE" }, ...distributors]}
            defaultValue={v.distributorId ?? (distributors.length === 1 ? distributors[0]?.value : "")}
            description="Solo verá las solicitudes, la red y los reportes de esta EDE."
            errors={e.distributorId}
          />
        )}
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField
          name="password"
          idPrefix="usuario"
          label="Contraseña temporal"
          type="password"
          autoComplete="new-password"
          required
          description={PASSWORD_HELP}
          errors={e.password}
        />
        <FormField
          name="confirmPassword"
          idPrefix="usuario"
          label="Repite la contraseña"
          type="password"
          autoComplete="new-password"
          required
          errors={e.confirmPassword}
        />
      </div>
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Creando…" : "Crear usuario"}
      </Button>
    </form>
  );
}

/** Desactivar pide confirmación; reactivar no, porque no quita nada. */
export function UserStatusForm({ id, name, active, isSelf }: { id: string; name: string; active: boolean; isSelf: boolean }) {
  const [state, action, pending] = useActionState(setUserActiveAction, IDLE);
  const [confirming, setConfirming] = useState(false);

  if (isSelf) return <span className="text-sm text-muted-foreground">Tu cuenta</span>;

  return (
    <div className="grid justify-items-end gap-1">
      {active && !confirming ? (
        <Button type="button" variant="ghost" size="sm" className="text-destructive" onClick={() => setConfirming(true)}>
          Desactivar<span className="sr-only"> a {name}</span>
        </Button>
      ) : (
        <form action={action} className="flex flex-wrap items-center justify-end gap-2">
          <input type="hidden" name="id" value={id} />
          <input type="hidden" name="active" value={active ? "false" : "true"} />
          {active && <span className="text-sm">¿Desactivar a {name}?</span>}
          <Button type="submit" size="sm" variant={active ? "destructive" : "outline"} disabled={pending}>
            {active ? "Sí, desactivar" : "Reactivar"}
            {!active && <span className="sr-only"> a {name}</span>}
          </Button>
          {active && (
            <Button type="button" size="sm" variant="ghost" onClick={() => setConfirming(false)}>
              No
            </Button>
          )}
        </form>
      )}
      {state.status === "error" && (
        <p role="alert" className="text-sm text-destructive">
          {state.message}
        </p>
      )}
    </div>
  );
}

/** EDE de un ingeniero o una distribuidora. Al cambiarla, la persona tiene que volver a iniciar sesión. */
export function UserDistributorForm({ id, name, current, distributors }: { id: string; name: string; current: number | null; distributors: Option[] }) {
  const [state, action, pending] = useActionState(setUserDistributorAction, IDLE);
  const selectId = `ede-${id}`;

  return (
    <form action={action} className="grid gap-1">
      <input type="hidden" name="id" value={id} />
      <div className="flex items-center gap-2">
        <label htmlFor={selectId} className="sr-only">
          Distribuidora de {name}
        </label>
        <select
          id={selectId}
          name="distributorId"
          defaultValue={current ? String(current) : ""}
          className="h-8 min-w-32 rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring"
        >
          {!current && <option value="">Sin asignar</option>}
          {distributors.map((d) => (
            <option key={d.value} value={d.value}>
              {d.label}
            </option>
          ))}
        </select>
        <Button type="submit" size="sm" variant="outline" disabled={pending}>
          Asignar<span className="sr-only"> la distribuidora a {name}</span>
        </Button>
      </div>
      {state.status !== "idle" && (
        <p role={state.status === "error" ? "alert" : "status"} className={state.status === "error" ? "text-sm text-destructive" : "text-sm text-muted-foreground"}>
          {state.message}
        </p>
      )}
    </form>
  );
}
