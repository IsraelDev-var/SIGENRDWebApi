import { describe, expect, it } from "vitest";

import { formatDateTime, formatFileSize } from "@/lib/format";

import { documentFileError, emptyRequestForm, EMPTY_GENERATOR, parseRequestForm, stepOf, stepsFor, type RequestFormValues } from "./schemas";

const valid = (changes: Partial<RequestFormValues> = {}): RequestFormValues => ({
  ...emptyRequestForm({ latitude: 18.4861, longitude: -69.9312 }),
  requestedCapacityKva: "7.5",
  projectAddress: "Calle 1, Santo Domingo",
  declarationAccepted: true,
  ...changes,
});

const generator = {
  ...EMPTY_GENERATOR,
  manufacturer: "SolarCo",
  ratedPowerKva: "5",
  connectionVoltage: "240",
  nominalCurrent: "20",
  powerFactorMin: "0.9",
  powerFactorMax: "1",
  harmonicDistortionPercent: "3",
};

describe("formulario de solicitud", () => {
  it("una conexión pequeña de un dueño no pide ingeniero ni dueño", () => {
    const result = parseRequestForm(valid(), { needsHolder: false });
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.project.requestedCapacityKva).toBe(7.5);
    expect(result.project.latitude).toBeCloseTo(18.4861);
    expect(result.project.netMetering).toBeUndefined();
    expect(result.holder).toBeNull();
  });

  it("más de 10 kVA pide el ingeniero, como la API", () => {
    const result = parseRequestForm(valid({ requestedCapacityKva: "11" }), { needsHolder: false });
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(Object.keys(result.errors).sort()).toEqual(["engineerCodiaNumber", "engineerFullName"]);
  });

  it("si no es el dueño pide los datos del dueño", () => {
    const result = parseRequestForm(valid({ propertyRelation: "Tenant" }), { needsHolder: false });
    expect(result.ok ? [] : Object.keys(result.errors).sort()).toEqual(["ownerFullName", "ownerNationalId"]);
  });

  it("la medición neta pide contrato, medidor, ingeniero y equipos válidos", () => {
    const result = parseRequestForm(valid({ requestType: "NetMetering", generators: [{ ...EMPTY_GENERATOR }] }), { needsHolder: false });
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.errors).toHaveProperty("serviceContractNumber");
    expect(result.errors).toHaveProperty("meterNumber");
    expect(result.errors).toHaveProperty("installedCapacityKw");
    expect(result.errors).toHaveProperty(["generators.0.manufacturer"]);
    expect(stepOf("generators.0.manufacturer")).toBe("tecnico");
  });

  it("la medición neta completa arma el bloque de generación", () => {
    const result = parseRequestForm(
      valid({
        requestType: "NetMetering",
        serviceContractNumber: "NIC-1",
        meterNumber: "M-1",
        engineerFullName: "Ana Pérez",
        engineerCodiaNumber: "12345",
        installedCapacityKw: "5",
        generators: [generator],
      }),
      { needsHolder: false },
    );
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.project.netMetering?.generators[0]).toMatchObject({ manufacturer: "SolarCo", phases: 1, powerFactorMin: 0.9 });
  });

  it("el factor de potencia mínimo no supera al máximo", () => {
    const result = parseRequestForm(
      valid({
        requestType: "NetMetering",
        serviceContractNumber: "NIC-1",
        meterNumber: "M-1",
        engineerFullName: "Ana",
        engineerCodiaNumber: "1",
        installedCapacityKw: "5",
        generators: [{ ...generator, powerFactorMin: "1", powerFactorMax: "0.8" }],
      }),
      { needsHolder: false },
    );
    expect(result.ok ? {} : result.errors).toHaveProperty(["generators.0.powerFactorMax"]);
  });

  it("B-11: la instaladora debe indicar el titular", () => {
    const missing = parseRequestForm(valid(), { needsHolder: true });
    expect(missing.ok ? [] : Object.keys(missing.errors).sort()).toEqual(["holderFullName", "holderNationalIdOrRnc"]);
    expect(stepOf("holderFullName")).toBe("titular");

    const ok = parseRequestForm(valid({ holderFullName: "Luis Gómez", holderNationalIdOrRnc: "402-1234567-8" }), { needsHolder: true });
    expect(ok.ok && ok.holder).toMatchObject({ fullName: "Luis Gómez", nationalIdOrRnc: "402-1234567-8", email: null });
  });

  it("sin certificar la información no se guarda", () => {
    const result = parseRequestForm(valid({ declarationAccepted: false }), { needsHolder: false });
    expect(result.ok ? {} : result.errors).toHaveProperty("declarationAccepted");
    expect(stepOf("declarationAccepted")).toBe("confirmar");
  });

  it.each(["abc", "", "1,5", "-3"])("rechaza la capacidad %j", (capacity) => {
    const result = parseRequestForm(valid({ requestedCapacityKva: capacity }), { needsHolder: false });
    expect(result.ok ? {} : result.errors).toHaveProperty("requestedCapacityKva");
  });

  it("el paso del titular solo aparece para la instaladora", () => {
    expect(stepsFor(true)).toContain("titular");
    expect(stepsFor(false)).not.toContain("titular");
  });
});

describe("archivos de documentos (RN-18, CA-06)", () => {
  const file = (name: string, type: string, size = 1000) => ({ name, type, size });

  it.each([file("plano.pdf", "application/pdf"), file("cedula.JPG", "image/jpeg"), file("foto.png", "image/png")])("acepta $name", (f) => {
    expect(documentFileError(f)).toBeNull();
  });

  it.each([
    file("virus.exe", "application/x-msdownload"),
    file("virus.pdf.exe", "application/pdf"),
    file("plano.pdf", "application/octet-stream"),
    file("hoja.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
  ])("rechaza $name ($type)", (f) => {
    expect(documentFileError(f)).toBe("Solo se aceptan archivos PDF, JPG o PNG.");
  });

  it("rechaza más de 10 MB y acepta justo 10 MB", () => {
    expect(documentFileError(file("plano.pdf", "application/pdf", 10 * 1024 * 1024 + 1))).toMatch(/10 MB/);
    expect(documentFileError(file("plano.pdf", "application/pdf", 10 * 1024 * 1024))).toBeNull();
  });

  it("pide elegir un archivo", () => {
    expect(documentFileError(null)).toBe("Elige un archivo.");
    expect(documentFileError(file("vacio.pdf", "application/pdf", 0))).toBe("Elige un archivo.");
  });
});

describe("formatos del expediente", () => {
  it("muestra el tamaño en KB o MB", () => {
    expect(formatFileSize(500)).toBe("1 KB");
    expect(formatFileSize(850 * 1024)).toBe("850 KB");
    expect(formatFileSize(2.4 * 1024 * 1024)).toBe("2.4 MB");
  });

  it("muestra la hora de República Dominicana", () => {
    expect(formatDateTime("2026-10-12T16:30:00Z")).toMatch(/12.*oct.*2026.*12:30/);
  });
});
