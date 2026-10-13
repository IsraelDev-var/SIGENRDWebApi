import { z } from "zod";

import type { components } from "@/lib/api/schema";

/**
 * Formulario de solicitud: repite las reglas de CreateConnectionRequestValidator para avisar antes de enviar y para
 * saber en qué paso del asistente está cada error. La API vuelve a validar todo.
 */

type Schemas = components["schemas"];
export type ProjectBody = Omit<Schemas["CreateConnectionRequestCommand"], "customerId" | "installerCompanyId" | "holder" | "distributorId" | "transformerId">;
export type HolderBody = Schemas["HolderInput"];

/** Hasta este valor, una conexión residencial se contrata directo; por encima pide ingeniero (RequiredDocuments.DirectContractMaxKva). */
export const DIRECT_CONTRACT_MAX_KVA = 10;

export type GeneratorValues = {
  generationSystemType: string;
  generatorKind: string;
  manufacturer: string;
  inverterModel: string;
  serialNumber: string;
  phases: string;
  ratedPowerKva: string;
  connectionVoltage: string;
  nominalCurrent: string;
  powerFactorMin: string;
  powerFactorMax: string;
  switchingType: string;
  harmonicDistortionPercent: string;
  certificationStandard: string;
  certifyingUnit: string;
};

/** Lo que guarda el asistente: texto tal como lo escribe la persona. */
export type RequestFormValues = {
  requestType: string;
  requestedCapacityKva: string;
  usageType: string;
  tariffType: string;
  interconnectionType: string;
  holderFullName: string;
  holderNationalIdOrRnc: string;
  holderIsLegalEntity: boolean;
  holderEmail: string;
  holderPhone: string;
  holderAddress: string;
  projectAddress: string;
  projectDescription: string;
  latitude: string;
  longitude: string;
  propertyRelation: string;
  ownerFullName: string;
  ownerNationalId: string;
  ownerPhone: string;
  engineerFullName: string;
  engineerCodiaNumber: string;
  engineerCompany: string;
  engineerPhone: string;
  engineerEmail: string;
  serviceContractNumber: string;
  meterNumber: string;
  generationSystemType: string;
  installedCapacityKw: string;
  netMeteringComments: string;
  generators: GeneratorValues[];
  declarationAccepted: boolean;
};

export const EMPTY_GENERATOR: GeneratorValues = {
  generationSystemType: "Photovoltaic",
  generatorKind: "InverterBased",
  manufacturer: "",
  inverterModel: "",
  serialNumber: "",
  phases: "1",
  ratedPowerKva: "",
  connectionVoltage: "",
  nominalCurrent: "",
  powerFactorMin: "",
  powerFactorMax: "",
  switchingType: "",
  harmonicDistortionPercent: "",
  certificationStandard: "",
  certifyingUnit: "",
};

export function emptyRequestForm(location: { latitude: number; longitude: number }): RequestFormValues {
  return {
    requestType: "Connection",
    requestedCapacityKva: "",
    usageType: "Residential",
    tariffType: "BT",
    interconnectionType: "Parallel",
    holderFullName: "",
    holderNationalIdOrRnc: "",
    holderIsLegalEntity: false,
    holderEmail: "",
    holderPhone: "",
    holderAddress: "",
    projectAddress: "",
    projectDescription: "",
    // El punto del transformador como referencia: la persona lo ajusta a su inmueble.
    latitude: location.latitude.toFixed(6),
    longitude: location.longitude.toFixed(6),
    propertyRelation: "Owner",
    ownerFullName: "",
    ownerNationalId: "",
    ownerPhone: "",
    engineerFullName: "",
    engineerCodiaNumber: "",
    engineerCompany: "",
    engineerPhone: "",
    engineerEmail: "",
    serviceContractNumber: "",
    meterNumber: "",
    generationSystemType: "Photovoltaic",
    installedCapacityKw: "",
    netMeteringComments: "",
    generators: [{ ...EMPTY_GENERATOR }],
    declarationAccepted: false,
  };
}

/** Valores del asistente a partir de un borrador guardado, para editarlo. */
export function formFromRequest(request: Schemas["ConnectionRequestResponseDto"]): RequestFormValues {
  const text = (value: string | number | null | undefined) => (value == null ? "" : String(value));
  const net = request.netMetering;
  return {
    ...emptyRequestForm({ latitude: request.latitude, longitude: request.longitude }),
    requestType: request.requestType,
    requestedCapacityKva: text(request.requestedCapacityKva),
    usageType: request.usageType,
    tariffType: request.tariffType,
    interconnectionType: request.interconnectionType,
    projectAddress: text(request.projectAddress),
    projectDescription: text(request.projectDescription),
    propertyRelation: request.propertyRelation,
    ownerFullName: text(request.ownerFullName),
    ownerNationalId: text(request.ownerNationalId),
    ownerPhone: text(request.ownerPhone),
    engineerFullName: text(request.engineerFullName),
    engineerCodiaNumber: text(request.engineerCodiaNumber),
    engineerCompany: text(request.engineerCompany),
    engineerPhone: text(request.engineerPhone),
    engineerEmail: text(request.engineerEmail),
    serviceContractNumber: text(request.serviceContractNumber),
    meterNumber: text(request.meterNumber),
    generationSystemType: net?.generationSystemType ?? "Photovoltaic",
    installedCapacityKw: text(net?.installedCapacityKw),
    netMeteringComments: text(net?.comments),
    generators: net?.generators.length
      ? net.generators.map((g) => ({
          generationSystemType: g.generationSystemType,
          generatorKind: g.generatorKind,
          manufacturer: g.manufacturer,
          inverterModel: text(g.inverterModel),
          serialNumber: text(g.serialNumber),
          phases: text(g.phases),
          ratedPowerKva: text(g.ratedPowerKva),
          connectionVoltage: text(g.connectionVoltage),
          nominalCurrent: text(g.nominalCurrent),
          powerFactorMin: text(g.powerFactorMin),
          powerFactorMax: text(g.powerFactorMax),
          switchingType: text(g.switchingType),
          harmonicDistortionPercent: text(g.harmonicDistortionPercent),
          certificationStandard: text(g.certificationStandard),
          certifyingUnit: text(g.certifyingUnit),
        }))
      : [{ ...EMPTY_GENERATOR }],
    // Al editar se vuelve a certificar la información.
    declarationAccepted: false,
  };
}

// ---------------- validación ----------------

const optional = (max: number) =>
  z
    .string()
    .trim()
    .max(max, `Máximo ${max} caracteres.`)
    .transform((v) => (v === "" ? null : v));

const required = (message: string, max: number) => z.string().trim().min(1, message).max(max, `Máximo ${max} caracteres.`);

/** Número escrito con punto decimal. Vacío o con letras: el mensaje indicado. */
const decimal = (message: string) =>
  z
    .string()
    .trim()
    .min(1, message)
    .regex(/^-?\d+(\.\d+)?$/, message)
    .transform(Number);

const optionalEmail = z
  .string()
  .trim()
  .transform((v) => (v === "" ? null : v))
  .pipe(z.email("Escribe un correo válido.").max(150, "Máximo 150 caracteres.").nullable());

const generatorSchema = z
  .object({
    generationSystemType: z.enum(["Photovoltaic", "Wind", "Biomass", "Hydro", "Hybrid", "Other"], "Elige la tecnología."),
    generatorKind: z.enum(["Synchronous", "Induction", "InverterBased", "Other"], "Elige el tipo de equipo."),
    manufacturer: required("Escribe el fabricante.", 150),
    inverterModel: optional(150),
    serialNumber: optional(150),
    phases: z.enum(["1", "3"], "Elige monofásica o trifásica.").transform(Number),
    ratedPowerKva: decimal("Escribe la potencia en kVA.").pipe(z.number().gt(0, "La potencia debe ser mayor que cero.")),
    connectionVoltage: decimal("Escribe el voltaje en V.").pipe(z.number().gt(0, "El voltaje debe ser mayor que cero.")),
    nominalCurrent: decimal("Escribe la corriente en A.").pipe(z.number().gt(0, "La corriente debe ser mayor que cero.")),
    powerFactorMin: decimal("Escribe el factor de potencia mínimo.").pipe(z.number().min(0.01, "Entre 0.01 y 1.").max(1, "Entre 0.01 y 1.")),
    powerFactorMax: decimal("Escribe el factor de potencia máximo.").pipe(z.number().min(0.01, "Entre 0.01 y 1.").max(1, "Entre 0.01 y 1.")),
    switchingType: optional(100),
    harmonicDistortionPercent: decimal("Escribe la distorsión armónica en %.").pipe(z.number().min(0, "Entre 0 y 100.").max(100, "Entre 0 y 100.")),
    certificationStandard: optional(100),
    certifyingUnit: optional(150),
  })
  .refine((g) => g.powerFactorMin <= g.powerFactorMax, { path: ["powerFactorMax"], message: "El máximo no puede ser menor que el mínimo." });

const baseSchema = z.object({
  requestType: z.enum(["Connection", "NetMetering"], "Elige el tipo de trámite."),
  requestedCapacityKva: decimal("Escribe la capacidad en kVA, por ejemplo 7.5.").pipe(
    z.number().gt(0, "La capacidad debe ser mayor que cero.").max(100_000, "La capacidad es demasiado alta."),
  ),
  usageType: z.enum(["Residential", "Commercial", "Industrial", "Government", "PublicService", "Agricultural"], "Elige el uso."),
  tariffType: z.enum(["BT", "MT", "AT"], "Elige la tarifa."),
  interconnectionType: z.enum(["Parallel", "MomentaryParallel", "Isolated"], "Elige el tipo de interconexión."),
  projectAddress: required("Escribe la dirección del proyecto.", 200),
  projectDescription: optional(1000),
  latitude: decimal("Escribe la latitud, por ejemplo 18.4861.").pipe(z.number().min(-90, "Entre -90 y 90.").max(90, "Entre -90 y 90.")),
  longitude: decimal("Escribe la longitud, por ejemplo -69.9312.").pipe(z.number().min(-180, "Entre -180 y 180.").max(180, "Entre -180 y 180.")),
  propertyRelation: z.enum(["Owner", "CoOwner", "Tenant", "Other"], "Elige tu relación con el inmueble."),
  ownerFullName: optional(150),
  ownerNationalId: optional(20),
  ownerPhone: optional(30),
  engineerFullName: optional(150),
  engineerCodiaNumber: optional(30),
  engineerCompany: optional(150),
  engineerPhone: optional(30),
  engineerEmail: optionalEmail,
  serviceContractNumber: optional(50),
  meterNumber: optional(50),
  declarationAccepted: z.literal(true, "Debes certificar que la información es correcta."),
});

const holderSchema = z.object({
  holderFullName: required("Escribe el nombre del titular.", 150),
  holderNationalIdOrRnc: required("Escribe la cédula o el RNC del titular.", 20),
  holderIsLegalEntity: z.boolean(),
  holderEmail: optionalEmail,
  holderPhone: optional(30),
  holderAddress: optional(250),
});

const netMeteringSchema = z.object({
  generationSystemType: z.enum(["Photovoltaic", "Wind", "Biomass", "Hydro", "Hybrid", "Other"], "Elige el sistema de generación."),
  installedCapacityKw: decimal("Escribe la capacidad instalada en kW.").pipe(z.number().gt(0, "La capacidad debe ser mayor que cero.")),
  netMeteringComments: optional(500),
  generators: z.array(generatorSchema).min(1, "Agrega al menos un generador o inversor."),
});

export type FieldErrors = Record<string, string[]>;

export type ParsedRequestForm =
  | { ok: true; project: ProjectBody; holder: HolderBody | null }
  | { ok: false; errors: FieldErrors };

function collect(errors: FieldErrors, issues: z.core.$ZodIssue[]) {
  for (const issue of issues) {
    const key = issue.path.join(".") || "form";
    errors[key] ??= [issue.message];
  }
}

/** Valida el asistente completo. `needsHolder`: la instaladora indica el titular (B-11). */
export function parseRequestForm(values: RequestFormValues, { needsHolder }: { needsHolder: boolean }): ParsedRequestForm {
  const errors: FieldErrors = {};
  const base = baseSchema.safeParse(values);
  if (!base.success) collect(errors, base.error.issues);

  const holder = needsHolder ? holderSchema.safeParse(values) : null;
  if (holder && !holder.success) collect(errors, holder.error.issues);

  const isNetMetering = values.requestType === "NetMetering";
  const net = isNetMetering ? netMeteringSchema.safeParse(values) : null;
  if (net && !net.success) collect(errors, net.error.issues);

  // Reglas que dependen de otros campos, como en la API.
  const need = (field: keyof RequestFormValues, message: string) => {
    if (typeof values[field] === "string" && !(values[field] as string).trim()) errors[field] ??= [message];
  };
  if (values.propertyRelation !== "Owner") {
    need("ownerFullName", "Indica el nombre del dueño del inmueble.");
    need("ownerNationalId", "Indica la cédula del dueño del inmueble.");
  }
  const capacity = Number(values.requestedCapacityKva);
  if (isNetMetering || (Number.isFinite(capacity) && capacity > DIRECT_CONTRACT_MAX_KVA)) {
    need("engineerFullName", "Indica el nombre del ingeniero responsable.");
    need("engineerCodiaNumber", "Indica el número de colegiatura CODIA del ingeniero.");
  }
  if (isNetMetering) {
    need("serviceContractNumber", "Indica el número de contrato del servicio.");
    need("meterNumber", "Indica el número del medidor.");
  }

  if (Object.keys(errors).length > 0 || !base.success) return { ok: false, errors };

  const { declarationAccepted, ...project } = base.data;
  const body: ProjectBody = {
    ...project,
    declarationAccepted,
    netMetering:
      net?.success
        ? {
            generationSystemType: net.data.generationSystemType,
            installedCapacityKw: net.data.installedCapacityKw,
            comments: net.data.netMeteringComments,
            generators: net.data.generators,
          }
        : // Sin la propiedad: la API la recibe como null en una conexión.
          undefined,
  };

  return {
    ok: true,
    project: body,
    holder:
      holder?.success
        ? {
            fullName: holder.data.holderFullName,
            nationalIdOrRnc: holder.data.holderNationalIdOrRnc,
            isLegalEntity: holder.data.holderIsLegalEntity,
            email: holder.data.holderEmail,
            phone: holder.data.holderPhone,
            address: holder.data.holderAddress,
          }
        : null,
  };
}

// ---------------- pasos del asistente ----------------

export type WizardStep = "tramite" | "titular" | "proyecto" | "tecnico" | "confirmar";

export const STEP_LABEL: Record<WizardStep, string> = {
  tramite: "Trámite",
  titular: "Titular",
  proyecto: "Proyecto",
  tecnico: "Datos técnicos",
  confirmar: "Confirmar",
};

export function stepsFor(needsHolder: boolean): WizardStep[] {
  return needsHolder ? ["tramite", "titular", "proyecto", "tecnico", "confirmar"] : ["tramite", "proyecto", "tecnico", "confirmar"];
}

const FIELD_STEP: [prefix: string, step: WizardStep][] = [
  ["requestType", "tramite"],
  ["requestedCapacityKva", "tramite"],
  ["usageType", "tramite"],
  ["tariffType", "tramite"],
  ["interconnectionType", "tramite"],
  ["holder", "titular"],
  ["project", "proyecto"],
  ["latitude", "proyecto"],
  ["longitude", "proyecto"],
  ["propertyRelation", "proyecto"],
  ["owner", "proyecto"],
  ["engineer", "tecnico"],
  ["serviceContractNumber", "tecnico"],
  ["meterNumber", "tecnico"],
  ["generationSystemType", "tecnico"],
  ["installedCapacityKw", "tecnico"],
  ["netMeteringComments", "tecnico"],
  ["generators", "tecnico"],
];

/** Paso del asistente donde está un campo; lo que no se reconoce (errores de la API) va al último. */
export function stepOf(field: string): WizardStep {
  return FIELD_STEP.find(([prefix]) => field.startsWith(prefix))?.[1] ?? "confirmar";
}

// ---------------- documentos ----------------

/** RN-18: PDF, JPG o PNG de hasta 10 MB. La API además revisa el contenido real del archivo. */
export const MAX_DOCUMENT_BYTES = 10 * 1024 * 1024;
const ACCEPTED_DOCUMENTS: Record<string, string[]> = {
  "application/pdf": [".pdf"],
  "image/jpeg": [".jpg", ".jpeg"],
  "image/png": [".png"],
};
export const DOCUMENT_ACCEPT = Object.entries(ACCEPTED_DOCUMENTS)
  .flatMap(([type, extensions]) => [type, ...extensions])
  .join(",");

/** Error del archivo elegido, o null si se puede subir. */
export function documentFileError(file: { name: string; size: number; type: string } | null | undefined): string | null {
  if (!file || file.size === 0) return "Elige un archivo.";
  const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  const extensions = ACCEPTED_DOCUMENTS[file.type];
  if (!extensions || !extensions.includes(extension)) return "Solo se aceptan archivos PDF, JPG o PNG.";
  if (file.size > MAX_DOCUMENT_BYTES) return "El archivo pasa de 10 MB. Redúcelo o divídelo e inténtalo de nuevo.";
  return null;
}
