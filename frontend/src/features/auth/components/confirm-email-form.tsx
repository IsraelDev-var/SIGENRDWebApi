"use client";

import Link from "next/link";
import { useActionState } from "react";

import { FormMessage } from "@/components/shared/form-field";
import { Button } from "@/components/ui/button";

import { confirmEmailAction } from "../actions";
import { INITIAL_FORM_STATE } from "../schemas";

/**
 * La confirmación se hace al pulsar el botón y no al abrir la página: los antivirus de correo
 * abren los enlaces por su cuenta y no deben poder gastar el código.
 */
export function ConfirmEmailForm({ userId, code }: { userId: string; code: string }) {
  const [state, action, pending] = useActionState(confirmEmailAction, INITIAL_FORM_STATE);

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

  return (
    <form action={action} className="grid gap-4">
      <FormMessage status={state.status} message={state.message} />
      <input type="hidden" name="userId" value={userId} />
      <input type="hidden" name="code" value={code} />
      <Button type="submit" disabled={pending}>
        {pending ? "Confirmando…" : "Confirmar mi correo"}
      </Button>
    </form>
  );
}
