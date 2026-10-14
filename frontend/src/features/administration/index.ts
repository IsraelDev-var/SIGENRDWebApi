// API pública de la feature de administración: usuarios internos (B-5) y auditoría (RF-40). Solo administrador.
export { INTERNAL_ROLES, listUsers, PAGE_SIZE, searchAudit, type AuditFilters, type InternalRole } from "./api";
export { CreateUserForm, UserStatusForm } from "./components/user-forms";
export { AUDIT_ACTION_LABEL, AUDIT_ENTITY_LABEL, auditActionLabel, auditEntityLabel, prettyJson, roleLabel } from "./labels";
