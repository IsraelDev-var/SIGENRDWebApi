"use client";

import { useActionState } from "react";

import { Button } from "@/components/ui/button";

import { markReadAction, type NotificationActionState } from "../actions";

const IDLE: NotificationActionState = { status: "idle" };

/** Marcar una como leída, o todas si no se pasa `id`. */
export function MarkReadButton({ id, title }: { id?: number; title?: string }) {
  const [state, action, pending] = useActionState(markReadAction, IDLE);
  return (
    <form action={action} className="grid justify-items-end gap-1">
      {id !== undefined && <input type="hidden" name="id" value={id} />}
      <Button type="submit" size="sm" variant={id === undefined ? "outline" : "ghost"} disabled={pending}>
        {id === undefined ? "Marcar todas como leídas" : "Marcar como leída"}
        {title && <span className="sr-only">: {title}</span>}
      </Button>
      {state.status === "error" && (
        <p role="alert" className="text-sm text-destructive">
          {state.message}
        </p>
      )}
    </form>
  );
}
