import { describe, expect, it } from "vitest";

import { safeRedirectPath } from "@/lib/safe-redirect";

describe("destino tras iniciar sesión", () => {
  it.each(["/portal", "/gestion/solicitudes?estado=Received", "/portal/solicitudes/12#documentos"])("acepta la ruta interna %s", (path) => {
    expect(safeRedirectPath(path, "/")).toBe(path);
  });

  it.each([
    "https://otro.example/portal",
    "//otro.example",
    "/\\otro.example",
    "/\t/otro.example",
    "javascript:alert(1)",
    "portal",
    "",
    undefined,
    null,
    42,
    "/" + "a".repeat(600),
  ])("rechaza %s", (value) => {
    expect(safeRedirectPath(value, "/inicio")).toBe("/inicio");
  });
});
