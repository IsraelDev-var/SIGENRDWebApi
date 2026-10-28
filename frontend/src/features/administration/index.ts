// API pública de la feature de administración: usuarios internos (B-5) y auditoría (RF-40). Solo administrador.
export {
  DISTRIBUTOR_TYPES,
  getManagedDistributor,
  INTERNAL_ROLES,
  listManagedDistributors,
  listUsers,
  PAGE_SIZE,
  searchAudit,
  STAFF_ROLES,
  type AuditFilters,
  type InternalRole,
  type ManagedDistributor,
} from "./api";
export { DistributorForm, DistributorStatusForm } from "./components/distributor-forms";
export { CreateUserForm, UserDistributorForm, UserStatusForm } from "./components/user-forms";
export { AUDIT_ACTION_LABEL, AUDIT_ENTITY_LABEL, auditActionLabel, auditEntityLabel, prettyJson, roleLabel } from "./labels";
