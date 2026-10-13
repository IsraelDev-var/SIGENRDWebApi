import type { Option } from "@/components/shared/form-controls";
import type { components } from "@/lib/api/schema";

/** Textos de los valores que la API devuelve como enum. El orden de cada lista es el de los selectores. */

type Schemas = components["schemas"];

export const REQUEST_TYPE_OPTIONS: Option<Schemas["RequestType"]>[] = [
  { value: "Connection", label: "Conexión nueva", hint: "Suministro eléctrico para un inmueble o proyecto." },
  { value: "NetMetering", label: "Medición neta", hint: "Conectar paneles solares u otra generación propia a la red." },
];

export const USAGE_TYPE_OPTIONS: Option<Schemas["UsageType"]>[] = [
  { value: "Residential", label: "Residencial" },
  { value: "Commercial", label: "Comercial" },
  { value: "Industrial", label: "Industrial" },
  { value: "Government", label: "Gobierno" },
  { value: "PublicService", label: "Servicio público" },
  { value: "Agricultural", label: "Agrícola" },
];

export const TARIFF_TYPE_OPTIONS: Option<Schemas["TariffType"]>[] = [
  { value: "BT", label: "Baja tensión (BT)" },
  { value: "MT", label: "Media tensión (MT)" },
  { value: "AT", label: "Alta tensión (AT)" },
];

export const INTERCONNECTION_TYPE_OPTIONS: Option<Schemas["InterconnectionType"]>[] = [
  { value: "Parallel", label: "En paralelo con la red" },
  { value: "MomentaryParallel", label: "Paralelo momentáneo" },
  { value: "Isolated", label: "Aislado de la red" },
];

export const PROPERTY_RELATION_OPTIONS: Option<Schemas["PropertyRelation"]>[] = [
  { value: "Owner", label: "Dueño" },
  { value: "CoOwner", label: "Copropietario" },
  { value: "Tenant", label: "Inquilino" },
  { value: "Other", label: "Otra relación" },
];

export const GENERATION_SYSTEM_OPTIONS: Option<Schemas["GenerationSystemType"]>[] = [
  { value: "Photovoltaic", label: "Solar fotovoltaica" },
  { value: "Wind", label: "Eólica" },
  { value: "Biomass", label: "Biomasa" },
  { value: "Hydro", label: "Hidráulica" },
  { value: "Hybrid", label: "Híbrida" },
  { value: "Other", label: "Otra" },
];

export const GENERATOR_KIND_OPTIONS: Option<Schemas["GeneratorKind"]>[] = [
  { value: "InverterBased", label: "Con inversor" },
  { value: "Synchronous", label: "Síncrono" },
  { value: "Induction", label: "De inducción" },
  { value: "Other", label: "Otro" },
];

export const DOCUMENT_TYPE_LABEL: Record<Schemas["DocumentType"], string> = {
  IdentityDocument: "Cédula del titular",
  TaxIdCard: "Tarjeta del RNC",
  ConstitutiveDocuments: "Documentos constitutivos",
  BoardMinutes: "Acta de asamblea",
  IndustrialRegistry: "Registro industrial",
  CompanyRegistration: "Registro mercantil",
  ProofOfOwnership: "Título de propiedad",
  OwnerAuthorization: "Autorización del dueño",
  Blueprint: "Plano",
  SitePlan: "Plano de ubicación",
  ApprovedPlans: "Planos aprobados",
  NoApprovedPlansLetter: "Carta de no tener planos aprobados",
  InterconnectionScheme: "Diagrama unifilar",
  InverterSpecification: "Ficha del inversor",
  EquipmentCertificate: "Certificado de los equipos",
  TechnicalSheet: "Ficha técnica",
  CalculationMemory: "Memoria de cálculo",
  DescriptiveMemory: "Memoria descriptiva",
  FeasibilityRequestForm: "Formulario de factibilidad",
  EngineerIdentityDocument: "Cédula del ingeniero",
  CODIALicense: "Carnet del CODIA",
  RequestLetter: "Carta de solicitud",
  Permit: "Permiso",
  Invoice: "Factura",
  ApprovalCertificate: "Certificado de aprobación",
  Other: "Otro documento",
};

export function labelOf<T extends string>(options: Option<T>[], value: string | null | undefined): string {
  return options.find((o) => o.value === value)?.label ?? value ?? "Sin dato";
}

export function documentTypeLabel(value: string): string {
  return Object.hasOwn(DOCUMENT_TYPE_LABEL, value) ? DOCUMENT_TYPE_LABEL[value as Schemas["DocumentType"]] : value;
}
