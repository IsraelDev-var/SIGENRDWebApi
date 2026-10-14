import { describe, expect, it } from "vitest";

import { errorsByLine } from "./import-errors";

describe("errores de la importación CSV", () => {
  it("separa la línea del mensaje y ordena por línea", () => {
    expect(
      errorsByLine([
        "Línea 7: la zona 99 no existe.",
        "Línea 3: ya existe un transformador con el código TR-1.",
        "linea 12: la latitud no es un número.",
      ]),
    ).toEqual([
      { line: 3, message: "ya existe un transformador con el código TR-1." },
      { line: 7, message: "la zona 99 no existe." },
      { line: 12, message: "la latitud no es un número." },
    ]);
  });

  it("los errores del archivo entero quedan sin línea y primero", () => {
    expect(errorsByLine(["Línea 2: código vacío.", "Faltan columnas obligatorias: latitude."])).toEqual([
      { line: null, message: "Faltan columnas obligatorias: latitude." },
      { line: 2, message: "código vacío." },
    ]);
  });
});
