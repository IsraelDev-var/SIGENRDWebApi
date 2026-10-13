"use client";

import Link from "next/link";
import { useActionState } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";

import { forgotPasswordAction, loginAction, registerAction, resetPasswordAction } from "../actions";
import { INITIAL_FORM_STATE } from "../schemas";

const PASSWORD_RULES = "Al menos 10 caracteres, con mayúscula, minúscula y número.";

// noValidate: los mensajes salen de la validación propia, en español y asociados a cada campo.

export function LoginForm({ next }: { next?: string }) {
  const [state, action, pending] = useActionState(loginAction, INITIAL_FORM_STATE);

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      {next && <input type="hidden" name="next" value={next} />}
      <FormField
        name="email"
        label="Correo"
        type="email"
        autoComplete="email"
        required
        defaultValue={state.values?.email}
        errors={state.fieldErrors?.email}
      />
      <FormField
        name="password"
        label="Contraseña"
        type="password"
        autoComplete="current-password"
        required
        errors={state.fieldErrors?.password}
      />
      <Button type="submit" disabled={pending}>
        {pending ? "Ingresando…" : "Ingresar"}
      </Button>
      <p className="text-sm">
        <Link href="/recuperar" className="text-link underline underline-offset-4 hover:no-underline">
          Olvidé mi contraseña
        </Link>
      </p>
    </form>
  );
}

export function RegisterForm() {
  const [state, action, pending] = useActionState(registerAction, INITIAL_FORM_STATE);

  if (state.status === "success") {
    return (
      <div className="grid gap-4">
        <FormMessage status="success" message={state.message} />
        <Button asChild variant="outline">
          <Link href="/ingresar">Ir a iniciar sesión</Link>
        </Button>
      </div>
    );
  }

  const userType = state.values?.userType;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <fieldset className="grid gap-2" aria-describedby={state.fieldErrors?.userType ? "registro-tipo-error" : undefined}>
        <legend className="mb-1 text-sm font-medium">Me registro como</legend>
        <div className="grid gap-2 sm:grid-cols-2">
          {[
            { value: "Customer", label: "Cliente", hint: "Para tu casa o negocio." },
            { value: "InstallerCompany", label: "Empresa instaladora", hint: "Edesur debe validarla antes de solicitar." },
          ].map((option) => (
            <Label
              key={option.value}
              className="flex cursor-pointer items-start gap-2 rounded-md border bg-card p-3 font-normal has-checked:border-ring has-checked:bg-accent"
            >
              <input
                type="radio"
                name="userType"
                value={option.value}
                defaultChecked={userType ? userType === option.value : option.value === "Customer"}
                className="mt-1 accent-primary"
              />
              <span className="grid gap-0.5">
                <span className="font-medium">{option.label}</span>
                <span className="text-muted-foreground">{option.hint}</span>
              </span>
            </Label>
          ))}
        </div>
        {state.fieldErrors?.userType && (
          <p id="registro-tipo-error" className="text-sm text-destructive">
            {state.fieldErrors.userType.join(" ")}
          </p>
        )}
      </fieldset>
      <FormField
        name="fullName"
        label="Nombre completo"
        autoComplete="name"
        required
        defaultValue={state.values?.fullName}
        errors={state.fieldErrors?.fullName}
      />
      <FormField
        name="email"
        label="Correo"
        type="email"
        autoComplete="email"
        required
        defaultValue={state.values?.email}
        errors={state.fieldErrors?.email}
      />
      <FormField
        name="password"
        label="Contraseña"
        type="password"
        autoComplete="new-password"
        required
        description={PASSWORD_RULES}
        errors={state.fieldErrors?.password}
      />
      <FormField
        name="confirmPassword"
        label="Repite la contraseña"
        type="password"
        autoComplete="new-password"
        required
        errors={state.fieldErrors?.confirmPassword}
      />
      <Button type="submit" disabled={pending}>
        {pending ? "Creando cuenta…" : "Crear cuenta"}
      </Button>
    </form>
  );
}

export function ForgotPasswordForm() {
  const [state, action, pending] = useActionState(forgotPasswordAction, INITIAL_FORM_STATE);

  if (state.status === "success") return <FormMessage status="success" message={state.message} />;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <FormField
        name="email"
        label="Correo de tu cuenta"
        type="email"
        autoComplete="email"
        required
        defaultValue={state.values?.email}
        errors={state.fieldErrors?.email}
      />
      <Button type="submit" disabled={pending}>
        {pending ? "Enviando…" : "Enviar enlace"}
      </Button>
    </form>
  );
}

export function ResetPasswordForm({ email, code }: { email: string; code: string }) {
  const [state, action, pending] = useActionState(resetPasswordAction, INITIAL_FORM_STATE);

  if (state.status === "success") {
    return (
      <div className="grid gap-4">
        <FormMessage status="success" message={state.message} />
        <Button asChild>
          <Link href="/ingresar">Iniciar sesión</Link>
        </Button>
      </div>
    );
  }

  const linkError = state.fieldErrors?.email ?? state.fieldErrors?.code;

  return (
    <form action={action} noValidate className="grid gap-4">
      <FormMessage status={state.status} message={linkError ? "El enlace no es válido. Pide uno nuevo." : state.message} />
      <input type="hidden" name="email" value={email} />
      <input type="hidden" name="code" value={code} />
      <p className="text-sm text-muted-foreground">
        Cuenta: <span className="font-medium text-foreground">{email}</span>
      </p>
      <FormField
        name="password"
        label="Contraseña nueva"
        type="password"
        autoComplete="new-password"
        required
        description={PASSWORD_RULES}
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
        {pending ? "Guardando…" : "Guardar contraseña"}
      </Button>
    </form>
  );
}
