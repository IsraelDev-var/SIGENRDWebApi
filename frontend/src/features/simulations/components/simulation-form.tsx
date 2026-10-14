"use client";

import { Info, Plus, Trash2 } from "lucide-react";
import { useState, useTransition, type ComponentProps } from "react";

import { FormField, FormMessage } from "@/components/shared/form-field";
import { ChoiceCards, SelectField } from "@/components/shared/form-controls";
import { Button } from "@/components/ui/button";

import { createSimulationAction } from "../actions";
import { EMPTY_PANEL, MAX_EQUIPMENT, parseSimulationForm, type EquipmentValues, type FieldErrors, type SimulationFormValues } from "../schemas";

const MODE_OPTIONS = [
  { value: "capacity" as const, label: "Sé la potencia", hint: "Escribe los kWp que te cotizaron o que quieres instalar." },
  { value: "equipment" as const, label: "Tengo los equipos", hint: "Registra paneles e inversores; la potencia se suma sola." },
];

/** Simulador de medición neta (RF-18 a RF-24). Solo recoge los datos: el cálculo y la clasificación son de la API. */
export function SimulationForm({ initialValues }: { initialValues: SimulationFormValues }) {
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();

  function set<K extends keyof SimulationFormValues>(key: K, value: SimulationFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }));
  }

  function setRow(i: number, key: keyof EquipmentValues, value: string) {
    setValues((current) => ({ ...current, equipment: current.equipment.map((row, n) => (n === i ? { ...row, [key]: value } : row)) }));
  }

  function submit() {
    const parsed = parseSimulationForm(values);
    if (!parsed.ok) {
      setErrors(parsed.errors);
      setMessage("Revisa los campos marcados.");
      return;
    }
    setErrors({});
    setMessage(undefined);
    startTransition(async () => {
      const state = await createSimulationAction(values);
      // Si se calculó, la acción lleva al resultado.
      setErrors(state.fieldErrors ?? {});
      setMessage(state.message);
    });
  }

  const field = (name: "name" | "monthlyConsumptionKwh" | "latitude" | "longitude" | "installedCapacityKw" | "investmentCost" | "flatPricePerKwh", label: string, props: Partial<ComponentProps<typeof FormField>> = {}) => (
    <FormField name={name} label={label} value={values[name]} onChange={(e) => set(name, e.target.value)} errors={errors[name]} {...props} />
  );

  return (
    <form
      noValidate
      className="grid gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        submit();
      }}
    >
      <FormMessage status={message ? "error" : "idle"} message={message} />

      <fieldset className="grid gap-4">
        <legend className="mb-2 text-lg font-semibold">Tu consumo</legend>
        {field("monthlyConsumptionKwh", "Consumo mensual (kWh)", {
          inputMode: "decimal",
          required: true,
          description: "Está en tu factura de Edesur. Si varía mucho, usa el promedio de los últimos 12 meses.",
        })}
        <div className="grid gap-4 sm:grid-cols-2">
          {field("latitude", "Latitud", { inputMode: "decimal", required: true })}
          {field("longitude", "Longitud", { inputMode: "decimal", required: true })}
        </div>
        <p className="text-sm text-muted-foreground">
          La ubicación sirve para buscar el transformador más cercano como referencia. Si llegaste desde el mapa, ya viene puesta.
        </p>
      </fieldset>

      <fieldset className="grid gap-4">
        <legend className="mb-2 text-lg font-semibold">El sistema solar</legend>
        <ChoiceCards name="mode" legend="¿Qué datos tienes?" options={MODE_OPTIONS} value={values.mode} onChange={(v) => set("mode", v)} />

        {values.mode === "capacity" ? (
          field("installedCapacityKw", "Potencia estimada (kWp)", { inputMode: "decimal", required: true })
        ) : (
          <div className="grid gap-3">
            {errors.equipment && (
              <p role="alert" className="text-sm text-destructive">
                {errors.equipment.join(" ")}
              </p>
            )}
            {values.equipment.map((row, i) => (
              <fieldset key={i} className="grid gap-3 rounded-lg border p-4">
                <legend className="px-1 text-sm font-medium">Equipo {i + 1}</legend>
                <div className="grid gap-3 sm:grid-cols-[10rem_1fr]">
                  <SelectField
                    id={`equipo-${i}-tipo`}
                    label="Tipo"
                    options={[
                      { value: "panel", label: "Paneles" },
                      { value: "inverter", label: "Inversor" },
                    ]}
                    value={row.kind}
                    onChange={(e) => setRow(i, "kind", e.target.value)}
                  />
                  <FormField
                    name={`equipment.${i}.model`}
                    idPrefix="equipo"
                    label={row.kind === "panel" ? "Modelo del panel (opcional)" : "Modelo del inversor"}
                    value={row.model}
                    onChange={(e) => setRow(i, "model", e.target.value)}
                    errors={errors[`equipment.${i}.model`]}
                  />
                </div>
                <div className="grid gap-3 sm:grid-cols-2">
                  <FormField
                    name={`equipment.${i}.ratedPowerKw`}
                    idPrefix="equipo"
                    label={row.kind === "panel" ? "Potencia de cada panel (kW)" : "Potencia del inversor (kW)"}
                    description={row.kind === "panel" ? "Un panel de 550 W son 0.55 kW." : undefined}
                    inputMode="decimal"
                    value={row.ratedPowerKw}
                    onChange={(e) => setRow(i, "ratedPowerKw", e.target.value)}
                    errors={errors[`equipment.${i}.ratedPowerKw`]}
                  />
                  <FormField
                    name={`equipment.${i}.quantity`}
                    idPrefix="equipo"
                    label="Cantidad"
                    inputMode="numeric"
                    value={row.quantity}
                    onChange={(e) => setRow(i, "quantity", e.target.value)}
                    errors={errors[`equipment.${i}.quantity`]}
                  />
                </div>
                {values.equipment.length > 1 && (
                  <Button
                    type="button"
                    variant="ghost"
                    className="justify-self-start text-destructive"
                    onClick={() => set("equipment", values.equipment.filter((_, n) => n !== i))}
                  >
                    <Trash2 aria-hidden="true" /> Quitar el equipo {i + 1}
                  </Button>
                )}
              </fieldset>
            ))}
            {values.equipment.length < MAX_EQUIPMENT && (
              <Button type="button" variant="outline" className="justify-self-start" onClick={() => set("equipment", [...values.equipment, { ...EMPTY_PANEL }])}>
                <Plus aria-hidden="true" /> Agregar equipo
              </Button>
            )}
          </div>
        )}
      </fieldset>

      <details className="rounded-lg border p-4">
        <summary className="cursor-pointer font-medium">Datos opcionales: cotización y tarifa</summary>
        <div className="mt-4 grid gap-4 sm:grid-cols-2">
          {field("investmentCost", "Costo de la cotización (RD$)", {
            inputMode: "decimal",
            description: "Si lo dejas vacío, se estima con un costo promedio por kWp.",
          })}
          {field("flatPricePerKwh", "Precio fijo por kWh (RD$)", {
            inputMode: "decimal",
            description: "Solo para tarifas comerciales o industriales. Vacío: tarifa residencial BTS-1.",
          })}
        </div>
      </details>

      {field("name", "Nombre para recordarla (opcional)", { placeholder: "Casa, techo del local…", maxLength: 100 })}

      <p className="flex items-start gap-2 text-sm text-muted-foreground">
        <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
        La simulación es informativa: no es una solicitud, no tiene valor regulatorio y no reserva capacidad del transformador.
      </p>

      <Button type="submit" disabled={pending} className="justify-self-start">
        {pending ? "Calculando…" : "Calcular"}
      </Button>
    </form>
  );
}
