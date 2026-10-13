import { Ban, CircleAlert, CircleCheck, TriangleAlert, Wrench, type LucideIcon } from "lucide-react";

import type { components } from "@/lib/api/schema";

/**
 * Cómo se muestra cada estado que calcula la API. El frontend nunca deduce el estado del porcentaje:
 * los umbrales y el factor de potencia son configurables por distribuidora (RN-01, RN-02).
 */

export type TransformerStatus = components["schemas"]["TransformerStatus"];

/** En el orden del semáforo (RN-02). */
export const TRANSFORMER_STATUSES = ["Available", "Conditioned", "Critical", "Saturated", "Maintenance"] as const satisfies readonly TransformerStatus[];
export type RequestStatus = components["schemas"]["RequestStatus"];

export type StatusAppearance = {
  label: string;
  /** Qué significa para el solicitante; se usa en leyendas y textos de ayuda. */
  description: string;
  icon: LucideIcon;
  /** Clases de Tailwind de los tokens de globals.css. */
  fillClass: string;
  badgeClass: string;
};

// Crítica y Saturada comparten el rojo pero no la forma ni el icono: tienen reglas distintas (RN-02, RN-03, RN-15).
export const TRANSFORMER_STATUS: Record<TransformerStatus, StatusAppearance> = {
  Available: {
    label: "Disponible",
    description: "70 % o más de capacidad libre. Acepta solicitudes.",
    icon: CircleCheck,
    fillClass: "bg-status-available",
    badgeClass: "bg-status-available-soft text-status-available-text",
  },
  Conditioned: {
    label: "Condicionada",
    description: "Entre 30 % y menos de 70 % libre. Acepta solicitudes.",
    icon: CircleAlert,
    fillClass: "bg-status-conditioned",
    badgeClass: "bg-status-conditioned-soft text-status-conditioned-text",
  },
  Critical: {
    label: "Crítica",
    description: "Menos de 30 % libre. Acepta solicitudes solo si la capacidad disponible cubre la requerida.",
    icon: TriangleAlert,
    fillClass: "bg-status-critical",
    badgeClass: "bg-status-critical-soft text-status-critical-text",
  },
  Saturated: {
    label: "Saturada",
    description: "Sin capacidad libre. No acepta solicitudes nuevas.",
    icon: Ban,
    fillClass: "bg-status-saturated",
    badgeClass: "bg-status-saturated-soft text-status-saturated-text",
  },
  Maintenance: {
    label: "Mantenimiento",
    description: "En mantenimiento por decisión de la distribuidora. No acepta solicitudes.",
    icon: Wrench,
    fillClass: "bg-status-maintenance",
    badgeClass: "bg-status-maintenance-soft text-status-maintenance-text",
  },
};

export type Tone = "info" | "review" | "warning" | "success" | "danger" | "neutral";

export const TONE_BADGE_CLASS: Record<Tone, string> = {
  info: "bg-tone-info-soft text-tone-info-text",
  review: "bg-tone-review-soft text-tone-review-text",
  warning: "bg-tone-warning-soft text-tone-warning-text",
  success: "bg-tone-success-soft text-tone-success-text",
  danger: "bg-tone-danger-soft text-tone-danger-text",
  neutral: "bg-tone-neutral-soft text-tone-neutral-text",
};

export const REQUEST_STATUS: Record<RequestStatus, { label: string; tone: Tone; meaning: string }> = {
  Draft: {
    label: "Borrador",
    tone: "neutral",
    meaning: "Todavía no se envió: solo la ve quien la creó. Sube los documentos y envíala para que Edesur la reciba.",
  },
  Received: { label: "Recibida", tone: "info", meaning: "Edesur la recibió y la revisará. Si falta algo, te avisará por correo." },
  UnderReview: {
    label: "En revisión",
    tone: "review",
    meaning: "El personal de Edesur la está revisando. La capacidad pedida quedó reservada en el transformador.",
  },
  Observed: { label: "Observaciones", tone: "warning", meaning: "Edesur pidió correcciones. Entra a tu portal para ver qué falta." },
  InCorrection: { label: "En corrección", tone: "warning", meaning: "El solicitante está corrigiendo la solicitud para reenviarla." },
  Approved: { label: "Aprobada", tone: "success", meaning: "Edesur la aprobó. El certificado está disponible en el portal del solicitante." },
  Rejected: { label: "Rechazada", tone: "danger", meaning: "Edesur la rechazó. El motivo está en el portal del solicitante." },
  Cancelled: { label: "Cancelada", tone: "neutral", meaning: "El solicitante la canceló." },
};

export const REQUEST_TYPE_LABEL: Record<components["schemas"]["RequestType"], string> = {
  Connection: "Conexión nueva",
  NetMetering: "Medición neta",
};

// Algunos DTO devuelven el estado como texto libre (string), no como enum: se valida antes de usarlo.
export function asTransformerStatus(value: string | null | undefined): TransformerStatus | null {
  return value != null && Object.hasOwn(TRANSFORMER_STATUS, value) ? (value as TransformerStatus) : null;
}

export function asRequestStatus(value: string | null | undefined): RequestStatus | null {
  return value != null && Object.hasOwn(REQUEST_STATUS, value) ? (value as RequestStatus) : null;
}
