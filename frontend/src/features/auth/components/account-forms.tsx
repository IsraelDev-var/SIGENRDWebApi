"use client";

import { useActionState } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { Button } from "@/components/ui/button";

import { changePasswordAction, updateCustomerProfileAction, updateInstallerCompanyAction } from "../actions";
import { INITIAL_FORM_STATE } from "../schemas";

const PASSWORD_HELP = "Al menos 10 caracteres, con una mayúscula, una minúscula y un número.";

/** Cambiar la contraseña; obligatorio cuando la puso un administrador. */
export function ChangePasswordForm() {
  const [state, action, pending] = useActionState(changePasswordAction, INITIAL_FORM_STATE);

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <FormField
        name="currentPassword"
        label="Contraseña actual"
        type="password"
        autoComplete="current-password"
        required
        errors={state.fieldErrors?.currentPassword}
      />
      <FormField
        name="password"
        label="Contraseña nueva"
        type="password"
        autoComplete="new-password"
        required
        description={PASSWORD_HELP}
        errors={state.fieldErrors?.password}
      />
      <FormField
        name="confirmPassword"
        label="Repite la contraseña nueva"
        type="password"
        autoComplete="new-password"
        required
        errors={state.fieldErrors?.confirmPassword}
      />
      <Button type="submit" disabled={pending}>
        {pending ? "Guardando…" : "Cambiar contraseña"}
      </Button>
    </form>
  );
}

type CustomerValues = { fullName: string; email?: string | null; phone?: string | null; address?: string | null };

/** Editar el perfil del cliente (B-6, RF-09). La cédula se muestra aparte: no se edita aquí. */
export function EditCustomerProfileForm({ profile }: { profile: CustomerValues }) {
  const [state, action, pending] = useActionState(updateCustomerProfileAction, INITIAL_FORM_STATE);
  const v = state.values;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <FormField name="fullName" label="Nombre del titular" autoComplete="name" required defaultValue={v?.fullName ?? profile.fullName} errors={state.fieldErrors?.fullName} />
      <FormField
        name="email"
        label="Correo de contacto (opcional)"
        type="email"
        autoComplete="email"
        defaultValue={v?.email ?? profile.email ?? ""}
        errors={state.fieldErrors?.email}
      />
      <FormField name="phone" label="Teléfono (opcional)" type="tel" autoComplete="tel" defaultValue={v?.phone ?? profile.phone ?? ""} errors={state.fieldErrors?.phone} />
      <FormField
        name="address"
        label="Dirección (opcional)"
        autoComplete="street-address"
        defaultValue={v?.address ?? profile.address ?? ""}
        errors={state.fieldErrors?.address}
      />
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Guardando…" : "Guardar cambios"}
      </Button>
    </form>
  );
}

type CompanyValues = { tradeName: string; contactEmail?: string | null; contactPhone?: string | null; address?: string | null };

/** Editar los datos de la empresa instaladora (B-6). El RNC se muestra aparte: no se edita. */
export function EditInstallerCompanyForm({ company }: { company: CompanyValues }) {
  const [state, action, pending] = useActionState(updateInstallerCompanyAction, INITIAL_FORM_STATE);
  const v = state.values;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <FormField
        name="tradeName"
        label="Nombre comercial"
        autoComplete="organization"
        required
        defaultValue={v?.tradeName ?? company.tradeName}
        errors={state.fieldErrors?.tradeName}
      />
      <FormField
        name="contactEmail"
        label="Correo de contacto (opcional)"
        type="email"
        autoComplete="email"
        defaultValue={v?.contactEmail ?? company.contactEmail ?? ""}
        errors={state.fieldErrors?.contactEmail}
      />
      <FormField
        name="contactPhone"
        label="Teléfono (opcional)"
        type="tel"
        autoComplete="tel"
        defaultValue={v?.contactPhone ?? company.contactPhone ?? ""}
        errors={state.fieldErrors?.contactPhone}
      />
      <FormField
        name="address"
        label="Dirección (opcional)"
        autoComplete="street-address"
        defaultValue={v?.address ?? company.address ?? ""}
        errors={state.fieldErrors?.address}
      />
      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Guardando…" : "Guardar cambios"}
      </Button>
    </form>
  );
}
