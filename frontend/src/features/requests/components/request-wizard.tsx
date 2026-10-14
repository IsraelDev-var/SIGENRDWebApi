"use client";

import { Plus, Trash2 } from "lucide-react";
import { useEffect, useRef, useState, useTransition, type ComponentProps } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { KvaValue } from "@/components/shared/kva-value";
import { TransformerStatusBadge } from "@/components/shared/transformer-status-badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

import { saveDraftAction } from "../actions";
import {
  GENERATION_SYSTEM_OPTIONS,
  GENERATOR_KIND_OPTIONS,
  INTERCONNECTION_TYPE_OPTIONS,
  labelOf,
  PROPERTY_RELATION_OPTIONS,
  REQUEST_TYPE_OPTIONS,
  TARIFF_TYPE_OPTIONS,
  USAGE_TYPE_OPTIONS,
} from "../labels";
import {
  DIRECT_CONTRACT_MAX_KVA,
  EMPTY_GENERATOR,
  parseRequestForm,
  STEP_LABEL,
  stepOf,
  stepsFor,
  type FieldErrors,
  type GeneratorValues,
  type RequestFormValues,
  type WizardStep,
} from "../schemas";
import { ChoiceCards, SelectField, TextareaField } from "@/components/shared/form-controls";

export type WizardTransformer = {
  id: number;
  code: string;
  distributorId: number;
  status: string;
  availableCapacityKva: number;
};

type Props = {
  transformer: WizardTransformer;
  initialValues: RequestFormValues;
  /** La instaladora indica el titular al crear (B-11). */
  needsHolder: boolean;
  /** Titular ya conocido: el perfil del cliente o el de un borrador. */
  holderName?: string | null;
  /** Al editar un borrador. */
  requestId?: number;
};

/**
 * Asistente de solicitud por pasos (RF-10). Guarda un borrador (D-4): los documentos se suben después, en el
 * expediente, y desde ahí se envía. Cada paso valida sus campos antes de avanzar; la API valida todo al guardar.
 */
export function RequestWizard({ transformer, initialValues, needsHolder, holderName, requestId }: Props) {
  const steps = stepsFor(needsHolder);
  const [step, setStep] = useState<WizardStep>(steps[0]!);
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();
  const headingRef = useRef<HTMLHeadingElement>(null);
  const firstRender = useRef(true);

  const index = steps.indexOf(step);
  const isNetMetering = values.requestType === "NetMetering";
  const capacity = Number(values.requestedCapacityKva);
  const needsEngineer = isNetMetering || (Number.isFinite(capacity) && capacity > DIRECT_CONTRACT_MAX_KVA);

  // Al cambiar de paso, el foco va al título del paso para que el lector de pantalla lo anuncie.
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    headingRef.current?.focus();
  }, [step]);

  function set<K extends keyof RequestFormValues>(key: K, value: RequestFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function setGenerator(i: number, key: keyof GeneratorValues, value: string) {
    setValues((current) => ({
      ...current,
      generators: current.generators.map((g, n) => (n === i ? { ...g, [key]: value } : g)),
    }));
  }

  function errorsFor(target: WizardStep): FieldErrors {
    const result = parseRequestForm(values, { needsHolder });
    if (result.ok) return {};
    return Object.fromEntries(Object.entries(result.errors).filter(([key]) => stepOf(key) === target));
  }

  function next() {
    const found = errorsFor(step);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      setMessage("Revisa los campos marcados antes de seguir.");
      return;
    }
    setMessage(undefined);
    setStep(steps[index + 1]!);
  }

  function back() {
    setErrors({});
    setMessage(undefined);
    setStep(steps[index - 1]!);
  }

  function save() {
    const result = parseRequestForm(values, { needsHolder });
    if (!result.ok) {
      showErrors(result.errors, "Revisa los campos marcados.");
      return;
    }
    startTransition(async () => {
      const state = await saveDraftAction({ values, transformerId: transformer.id, distributorId: transformer.distributorId, requestId });
      // Si se guardó, la acción redirige al expediente.
      if (state.status === "error") showErrors(state.fieldErrors ?? {}, state.message);
    });
  }

  /** Lleva al primer paso con errores, o se queda en el último si el error es general. */
  function showErrors(found: FieldErrors, text?: string) {
    setErrors(found);
    setMessage(text);
    const first = steps.find((s) => Object.keys(found).some((key) => stepOf(key) === s));
    if (first) setStep(first);
  }

  const field = (name: keyof RequestFormValues & string, label: string, props: Partial<ComponentProps<typeof FormField>> = {}) => (
    <FormField
      name={name}
      label={label}
      value={String(values[name] ?? "")}
      onChange={(e) => set(name, e.target.value as never)}
      errors={errors[name]}
      {...props}
    />
  );

  return (
    <div className="grid gap-6">
      <nav aria-label="Pasos de la solicitud">
        <ol className="flex flex-wrap gap-x-4 gap-y-2 text-sm">
          {steps.map((s, n) => (
            <li key={s} aria-current={s === step ? "step" : undefined} className="flex items-center gap-2">
              <span
                aria-hidden="true"
                className={cn(
                  "flex size-6 items-center justify-center rounded-full border text-xs tabular-nums",
                  n < index && "border-primary bg-primary/10",
                  s === step && "border-primary bg-primary text-primary-foreground",
                )}
              >
                {n + 1}
              </span>
              <span className={cn(s === step ? "font-medium" : "text-muted-foreground")}>
                {STEP_LABEL[s]}
                {n < index && <span className="sr-only"> (completado)</span>}
              </span>
            </li>
          ))}
        </ol>
      </nav>

      <section className="flex flex-wrap items-center gap-3 rounded-lg border bg-muted/40 p-3 text-sm" aria-label="Transformador elegido">
        <span className="text-muted-foreground">Transformador</span>
        <span className="font-mono font-semibold">{transformer.code}</span>
        <TransformerStatusBadge status={transformer.status} />
        <span className="text-muted-foreground">
          Disponible: <KvaValue value={transformer.availableCapacityKva} />
        </span>
      </section>

      <form
        noValidate
        className="grid gap-5"
        onSubmit={(e) => {
          e.preventDefault();
          if (step === "confirmar") save();
          else next();
        }}
      >
        <h2 ref={headingRef} tabIndex={-1} className="text-lg font-semibold outline-none">
          Paso {index + 1} de {steps.length}: {STEP_LABEL[step]}
        </h2>

        <FormMessage status={message ? "error" : "idle"} message={message} />

        {step === "tramite" && (
          <>
            <ChoiceCards
              name="requestType"
              legend="¿Qué trámite necesitas?"
              options={REQUEST_TYPE_OPTIONS}
              value={values.requestType}
              onChange={(v) => set("requestType", v)}
              errors={errors.requestType}
            />
            {field("requestedCapacityKva", "Capacidad requerida (kVA)", {
              inputMode: "decimal",
              required: true,
              description: `Hasta ${DIRECT_CONTRACT_MAX_KVA} kVA es una conexión directa; por encima se pide un ingeniero responsable.`,
            })}
            {transformer.status === "Critical" && Number.isFinite(capacity) && capacity > transformer.availableCapacityKva && (
              <p role="status" className="rounded-md bg-tone-warning-soft px-3 py-2 text-sm text-tone-warning-text">
                Este transformador está en estado crítico y su capacidad disponible no cubre lo que pides. Edesur podría no
                aprobar la solicitud: considera otro transformador cercano.
              </p>
            )}
            <div className="grid gap-4 sm:grid-cols-3">
              <SelectField id="campo-usageType" label="Uso" options={USAGE_TYPE_OPTIONS} value={values.usageType} onChange={(e) => set("usageType", e.target.value)} errors={errors.usageType} />
              <SelectField id="campo-tariffType" label="Tarifa" options={TARIFF_TYPE_OPTIONS} value={values.tariffType} onChange={(e) => set("tariffType", e.target.value)} errors={errors.tariffType} />
              <SelectField
                id="campo-interconnectionType"
                label="Interconexión"
                options={INTERCONNECTION_TYPE_OPTIONS}
                value={values.interconnectionType}
                onChange={(e) => set("interconnectionType", e.target.value)}
                errors={errors.interconnectionType}
              />
            </div>
          </>
        )}

        {step === "titular" && (
          <>
            <p className="text-sm text-muted-foreground">
              La solicitud queda a nombre del titular del servicio. Si ya existe un cliente con esa cédula o RNC, se usa ese
              registro; si no, se crea sin cuenta. Solo tu empresa verá la solicitud mientras sea un borrador.
            </p>
            {field("holderFullName", "Nombre del titular", { required: true, autoComplete: "off" })}
            {field("holderNationalIdOrRnc", "Cédula o RNC del titular", { required: true, inputMode: "numeric", autoComplete: "off" })}
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={values.holderIsLegalEntity}
                onChange={(e) => set("holderIsLegalEntity", e.target.checked)}
                className="size-4 accent-primary"
              />
              El titular es una empresa o institución (persona jurídica)
            </label>
            <div className="grid gap-4 sm:grid-cols-2">
              {field("holderEmail", "Correo del titular (opcional)", { type: "email", autoComplete: "off" })}
              {field("holderPhone", "Teléfono del titular (opcional)", { type: "tel", autoComplete: "off" })}
            </div>
            {field("holderAddress", "Dirección del titular (opcional)", { autoComplete: "off" })}
          </>
        )}

        {step === "proyecto" && (
          <>
            {field("projectAddress", "Dirección del proyecto", { required: true, autoComplete: "street-address" })}
            <TextareaField
              id="campo-projectDescription"
              label="Descripción (opcional)"
              description="Qué se va a conectar: vivienda, local, nave, sistema solar…"
              value={values.projectDescription}
              onChange={(e) => set("projectDescription", e.target.value)}
              errors={errors.projectDescription}
              maxLength={1000}
            />
            <fieldset className="grid gap-2">
              <legend className="mb-2 text-sm font-medium">Ubicación del inmueble</legend>
              <p className="text-sm text-muted-foreground">
                Empieza en el punto del transformador. Ajústala con las coordenadas de tu inmueble (en el mapa de tu teléfono,
                mantén presionado el lugar para verlas).
              </p>
              <div className="grid gap-4 sm:grid-cols-2">
                {field("latitude", "Latitud", { inputMode: "decimal", required: true })}
                {field("longitude", "Longitud", { inputMode: "decimal", required: true })}
              </div>
            </fieldset>
            <SelectField
              id="campo-propertyRelation"
              label="Relación del titular con el inmueble"
              options={PROPERTY_RELATION_OPTIONS}
              value={values.propertyRelation}
              onChange={(e) => set("propertyRelation", e.target.value)}
              errors={errors.propertyRelation}
            />
            {values.propertyRelation !== "Owner" && (
              <fieldset className="grid gap-4 rounded-lg border p-4">
                <legend className="px-1 text-sm font-medium">Dueño del inmueble</legend>
                <p className="text-sm text-muted-foreground">Se pedirá su autorización entre los documentos.</p>
                {field("ownerFullName", "Nombre del dueño", { required: true })}
                <div className="grid gap-4 sm:grid-cols-2">
                  {field("ownerNationalId", "Cédula del dueño", { required: true, inputMode: "numeric" })}
                  {field("ownerPhone", "Teléfono del dueño (opcional)", { type: "tel" })}
                </div>
              </fieldset>
            )}
          </>
        )}

        {step === "tecnico" && (
          <>
            <fieldset className="grid gap-4">
              <legend className="mb-2 text-sm font-medium">
                Ingeniero responsable {needsEngineer ? "" : "(opcional)"}
              </legend>
              {needsEngineer && (
                <p className="text-sm text-muted-foreground">
                  Obligatorio para medición neta y para más de {DIRECT_CONTRACT_MAX_KVA} kVA.
                </p>
              )}
              <div className="grid gap-4 sm:grid-cols-2">
                {field("engineerFullName", "Nombre del ingeniero", { required: needsEngineer })}
                {field("engineerCodiaNumber", "Colegiatura CODIA", { required: needsEngineer })}
                {field("engineerCompany", "Empresa (opcional)")}
                {field("engineerPhone", "Teléfono (opcional)", { type: "tel" })}
              </div>
              {field("engineerEmail", "Correo (opcional)", { type: "email" })}
            </fieldset>

            <fieldset className="grid gap-4">
              <legend className="mb-2 text-sm font-medium">Servicio actual {isNetMetering ? "" : "(opcional)"}</legend>
              <div className="grid gap-4 sm:grid-cols-2">
                {field("serviceContractNumber", "Número de contrato (NIC)", { required: isNetMetering })}
                {field("meterNumber", "Número del medidor", { required: isNetMetering })}
              </div>
            </fieldset>

            {isNetMetering && (
              <fieldset className="grid gap-4">
                <legend className="mb-2 text-sm font-medium">Sistema de generación</legend>
                <div className="grid gap-4 sm:grid-cols-2">
                  <SelectField
                    id="campo-generationSystemType"
                    label="Tipo de sistema"
                    options={GENERATION_SYSTEM_OPTIONS}
                    value={values.generationSystemType}
                    onChange={(e) => set("generationSystemType", e.target.value)}
                    errors={errors.generationSystemType}
                  />
                  {field("installedCapacityKw", "Capacidad instalada (kW)", { inputMode: "decimal", required: true })}
                </div>
                <TextareaField
                  id="campo-netMeteringComments"
                  label="Comentarios (opcional)"
                  value={values.netMeteringComments}
                  onChange={(e) => set("netMeteringComments", e.target.value)}
                  errors={errors.netMeteringComments}
                  maxLength={500}
                />
                {errors.generators && <p className="text-sm text-destructive">{errors.generators.join(" ")}</p>}
                {values.generators.map((generator, i) => (
                  <GeneratorFields
                    key={i}
                    index={i}
                    generator={generator}
                    errors={errors}
                    onChange={(key, value) => setGenerator(i, key, value)}
                    onRemove={values.generators.length > 1 ? () => set("generators", values.generators.filter((_, n) => n !== i)) : undefined}
                  />
                ))}
                <Button
                  type="button"
                  variant="outline"
                  className="justify-self-start"
                  onClick={() => set("generators", [...values.generators, { ...EMPTY_GENERATOR, generationSystemType: values.generationSystemType }])}
                >
                  <Plus aria-hidden="true" /> Agregar otro equipo
                </Button>
              </fieldset>
            )}
          </>
        )}

        {step === "confirmar" && (
          <>
            <dl className="grid gap-2 rounded-lg border bg-card p-4 text-sm">
              <Summary label="Trámite" value={labelOf(REQUEST_TYPE_OPTIONS, values.requestType)} />
              <Summary label="Capacidad" value={`${values.requestedCapacityKva} kVA`} />
              <Summary label="Titular" value={needsHolder ? values.holderFullName : (holderName ?? "Tu perfil")} />
              <Summary label="Dirección" value={values.projectAddress} />
              <Summary label="Uso" value={labelOf(USAGE_TYPE_OPTIONS, values.usageType)} />
              {needsEngineer && <Summary label="Ingeniero" value={values.engineerFullName} />}
              {isNetMetering && <Summary label="Generación" value={`${values.installedCapacityKw} kW, ${values.generators.length} equipo(s)`} />}
            </dl>
            <p className="text-sm text-muted-foreground">
              Se guardará como borrador: nadie más lo verá. Después subirás los documentos y enviarás la solicitud a Edesur.
            </p>
            <label className="flex items-start gap-2 text-sm">
              <input
                type="checkbox"
                checked={values.declarationAccepted}
                onChange={(e) => set("declarationAccepted", e.target.checked)}
                aria-invalid={errors.declarationAccepted ? true : undefined}
                aria-describedby={errors.declarationAccepted ? "declaracion-error" : undefined}
                className="mt-0.5 size-4 accent-primary"
              />
              Certifico que la información es correcta y que los documentos que suba serán auténticos.
            </label>
            {errors.declarationAccepted && (
              <p id="declaracion-error" className="text-sm text-destructive">
                {errors.declarationAccepted.join(" ")}
              </p>
            )}
          </>
        )}

        <div className="flex flex-wrap gap-2">
          {index > 0 && (
            <Button type="button" variant="outline" onClick={back} disabled={pending}>
              Atrás
            </Button>
          )}
          <Button type="submit" disabled={pending}>
            {step !== "confirmar" ? "Siguiente" : pending ? "Guardando…" : requestId ? "Guardar cambios" : "Guardar borrador"}
          </Button>
        </div>
      </form>
    </div>
  );
}

function Summary({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid gap-0.5 sm:grid-cols-[10rem_1fr]">
      <dt className="text-muted-foreground">{label}</dt>
      <dd>{value || "Sin dato"}</dd>
    </div>
  );
}

function GeneratorFields({
  index,
  generator,
  errors,
  onChange,
  onRemove,
}: {
  index: number;
  generator: GeneratorValues;
  errors: FieldErrors;
  onChange: (key: keyof GeneratorValues, value: string) => void;
  onRemove?: () => void;
}) {
  const prefix = `generators.${index}.`;
  const input = (key: keyof GeneratorValues, label: string, props: Partial<ComponentProps<typeof FormField>> = {}) => (
    <FormField
      name={`${prefix}${key}`}
      idPrefix="equipo"
      label={label}
      value={generator[key]}
      onChange={(e) => onChange(key, e.target.value)}
      errors={errors[`${prefix}${key}`]}
      {...props}
    />
  );

  return (
    <fieldset className="grid gap-4 rounded-lg border p-4">
      <legend className="px-1 text-sm font-medium">Equipo {index + 1}</legend>
      <div className="grid gap-4 sm:grid-cols-2">
        <SelectField
          id={`equipo-${prefix}generatorKind`}
          label="Tipo de equipo"
          options={GENERATOR_KIND_OPTIONS}
          value={generator.generatorKind}
          onChange={(e) => onChange("generatorKind", e.target.value)}
          errors={errors[`${prefix}generatorKind`]}
        />
        <SelectField
          id={`equipo-${prefix}phases`}
          label="Conexión"
          options={[
            { value: "1", label: "Monofásica" },
            { value: "3", label: "Trifásica" },
          ]}
          value={generator.phases}
          onChange={(e) => onChange("phases", e.target.value)}
          errors={errors[`${prefix}phases`]}
        />
        {input("manufacturer", "Fabricante", { required: true })}
        {input("inverterModel", "Modelo (opcional)")}
        {input("serialNumber", "Número de serie (opcional)")}
        {input("ratedPowerKva", "Potencia nominal (kVA)", { inputMode: "decimal", required: true })}
        {input("connectionVoltage", "Voltaje de interconexión (V)", { inputMode: "decimal", required: true })}
        {input("nominalCurrent", "Corriente nominal (A)", { inputMode: "decimal", required: true })}
        {input("powerFactorMin", "Factor de potencia mínimo", { inputMode: "decimal", required: true, placeholder: "0.9" })}
        {input("powerFactorMax", "Factor de potencia máximo", { inputMode: "decimal", required: true, placeholder: "1" })}
        {input("harmonicDistortionPercent", "Distorsión armónica (%)", { inputMode: "decimal", required: true })}
        {input("certificationStandard", "Norma de certificación (opcional)", { placeholder: "IEEE 1547, UL 1741…" })}
      </div>
      {onRemove && (
        <Button type="button" variant="ghost" className="justify-self-start text-destructive" onClick={onRemove}>
          <Trash2 aria-hidden="true" /> Quitar el equipo {index + 1}
        </Button>
      )}
    </fieldset>
  );
}
