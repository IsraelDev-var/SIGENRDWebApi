"use client";

import { useActionState } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { Button } from "@/components/ui/button";

import { createCustomerProfileAction, registerInstallerCompanyAction } from "../actions";
import { INITIAL_FORM_STATE } from "../schemas";

/** Primer paso del cliente: los datos del titular que llevará cada solicitud. */
export function CustomerProfileForm({ defaultName, defaultEmail }: { defaultName: string; defaultEmail: string }) {
  const [state, action, pending] = useActionState(createCustomerProfileAction, INITIAL_FORM_STATE);
  const values = state.values;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <FormField
        name="fullName"
        label="Nombre del titular"
        autoComplete="name"
        required
        defaultValue={values?.fullName ?? defaultName}
        errors={state.fieldErrors?.fullName}
      />
      <FormField
        name="nationalIdOrRnc"
        label="Cédula o RNC"
        required
        inputMode="numeric"
        description="Sin guiones o con ellos, como prefieras."
        defaultValue={values?.nationalIdOrRnc}
        errors={state.fieldErrors?.nationalIdOrRnc}
      />
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" name="isLegalEntity" defaultChecked={values?.isLegalEntity === "on"} className="size-4 accent-primary" />
        Es una empresa o institución (persona jurídica)
      </label>
      <FormField
        name="email"
        label="Correo de contacto (opcional)"
        type="email"
        autoComplete="email"
        defaultValue={values?.email ?? defaultEmail}
        errors={state.fieldErrors?.email}
      />
      <FormField name="phone" label="Teléfono (opcional)" type="tel" autoComplete="tel" defaultValue={values?.phone} errors={state.fieldErrors?.phone} />
      <FormField
        name="address"
        label="Dirección (opcional)"
        autoComplete="street-address"
        defaultValue={values?.address}
        errors={state.fieldErrors?.address}
      />
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Guardando…" : "Guardar perfil"}
      </Button>
    </form>
  );
}

/** Primer paso de la instaladora: registrar la empresa, que queda Pendiente hasta que Edesur la valide (RN-05). */
export function InstallerCompanyForm({ defaultEmail }: { defaultEmail: string }) {
  const [state, action, pending] = useActionState(registerInstallerCompanyAction, INITIAL_FORM_STATE);
  const values = state.values;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <FormField
        name="tradeName"
        label="Nombre comercial"
        autoComplete="organization"
        required
        defaultValue={values?.tradeName}
        errors={state.fieldErrors?.tradeName}
      />
      <FormField name="rnc" label="RNC" required inputMode="numeric" defaultValue={values?.rnc} errors={state.fieldErrors?.rnc} />
      <FormField
        name="contactEmail"
        label="Correo de contacto (opcional)"
        type="email"
        autoComplete="email"
        defaultValue={values?.contactEmail ?? defaultEmail}
        errors={state.fieldErrors?.contactEmail}
      />
      <FormField
        name="contactPhone"
        label="Teléfono (opcional)"
        type="tel"
        autoComplete="tel"
        defaultValue={values?.contactPhone}
        errors={state.fieldErrors?.contactPhone}
      />
      <FormField
        name="address"
        label="Dirección (opcional)"
        autoComplete="street-address"
        defaultValue={values?.address}
        errors={state.fieldErrors?.address}
      />
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Registrando…" : "Registrar empresa"}
      </Button>
    </form>
  );
}
