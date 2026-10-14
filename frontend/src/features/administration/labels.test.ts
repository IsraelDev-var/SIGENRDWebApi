import { describe, expect, it } from "vitest";

import { auditActionLabel, auditEntityLabel, prettyJson, roleLabel } from "./labels";

describe("textos de la administración", () => {
  it("traduce acciones, entidades y roles conocidos y deja el resto tal cual", () => {
    expect(auditActionLabel("AccountDeactivated")).toBe("Desactivó la cuenta");
    expect(auditActionLabel("AlgoNuevo")).toBe("AlgoNuevo");
    expect(auditEntityLabel("ConnectionRequest")).toBe("Solicitud");
    expect(auditEntityLabel("toString")).toBe("toString");
    expect(roleLabel("Engineer")).toBe("Ingeniero");
  });

  it("da sangría al JSON de la auditoría sin romper lo que no es JSON", () => {
    expect(prettyJson('{"a":1}')).toBe('{\n  "a": 1\n}');
    expect(prettyJson("no json")).toBe("no json");
    expect(prettyJson(null)).toBeNull();
  });
});
