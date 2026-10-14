import { ROLE_LABEL, isRole } from "@/config/permissions";

/** Textos del registro de auditoría. Lo que no está aquí se muestra con el nombre técnico. */
export const AUDIT_ACTION_LABEL: Record<string, string> = {
  Added: "Creó",
  Modified: "Modificó",
  Deleted: "Eliminó",
  LoginSucceeded: "Inició sesión",
  LoginFailed: "Intento de inicio fallido",
  LoginLockedOut: "Cuenta bloqueada por intentos",
  LoginRejectedDeactivated: "Intento con cuenta desactivada",
  PasswordResetRequested: "Pidió restablecer la contraseña",
  PasswordResetCompleted: "Restableció la contraseña",
  AccountDeactivated: "Desactivó la cuenta",
  AccountReactivated: "Reactivó la cuenta",
};

export const AUDIT_ENTITY_LABEL: Record<string, string> = {
  Account: "Cuenta (seguridad)",
  ConnectionRequest: "Solicitud",
  StateHistory: "Historial de estados",
  TechnicalDocument: "Documento",
  Transformer: "Transformador",
  InstallerCompany: "Instaladora",
  Customer: "Cliente",
  NetMeteringRequest: "Medición neta",
  NetMeteringSimulation: "Simulación",
};

export function auditActionLabel(action: string): string {
  return Object.hasOwn(AUDIT_ACTION_LABEL, action) ? AUDIT_ACTION_LABEL[action]! : action;
}

export function auditEntityLabel(entity: string): string {
  return Object.hasOwn(AUDIT_ENTITY_LABEL, entity) ? AUDIT_ENTITY_LABEL[entity]! : entity;
}

export function roleLabel(role: string): string {
  return isRole(role) ? ROLE_LABEL[role] : role;
}

/** JSON de la auditoría con sangría para leerlo; si no es JSON válido se devuelve tal cual. */
export function prettyJson(value: string | null | undefined): string | null {
  if (!value) return null;
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
