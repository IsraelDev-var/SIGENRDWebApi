import { describe, expect, it, vi } from "vitest";

import { uploadDirect } from "./direct-upload";

const file = new File([new Uint8Array([37, 80, 68, 70])], "cedula.pdf", { type: "application/pdf" });

function respond(status: number, body?: unknown) {
  return vi.fn<typeof fetch>().mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } }),
  );
}

const input = (fetchImpl: typeof fetch) => ({
  uploadUrl: "https://api.ejemplo.do/api/v1/ConnectionRequest/7/documents/direct",
  ticket: "permiso",
  file,
  documentType: "IdentityDocument",
  fetchImpl,
});

describe("uploadDirect", () => {
  it("manda el archivo y el tipo a la API con el permiso, sin cookies", async () => {
    const fetchImpl = respond(200, { succeeded: true, data: { id: 1 } });
    expect(await uploadDirect(input(fetchImpl))).toEqual({ ok: true });

    const [url, init] = fetchImpl.mock.calls[0]!;
    expect(url).toBe("https://api.ejemplo.do/api/v1/ConnectionRequest/7/documents/direct");
    expect(init?.method).toBe("POST");
    expect(init?.credentials).toBe("omit");
    expect((init?.headers as Record<string, string>).Authorization).toBe("Bearer permiso");
    const body = init?.body as FormData;
    expect((body.get("File") as File).name).toBe("cedula.pdf");
    expect(body.get("DocumentType")).toBe("IdentityDocument");
  });

  it("muestra los errores de validación de la API", async () => {
    const result = await uploadDirect(input(respond(400, { succeeded: false, message: "Error", errors: ["El PDF contiene JavaScript."] })));
    expect(result).toEqual({ ok: false, message: "El PDF contiene JavaScript." });
  });

  it("explica el permiso vencido, la solicitud ajena, el archivo grande y el expediente bloqueado", async () => {
    expect(await uploadDirect(input(respond(401)))).toMatchObject({ message: expect.stringContaining("venció") });
    expect(await uploadDirect(input(respond(404)))).toMatchObject({ message: "La solicitud no existe o no es tuya." });
    expect(await uploadDirect(input(respond(413)))).toMatchObject({ message: expect.stringContaining("10 MB") });
    expect(await uploadDirect(input(respond(409, { message: "La solicitud está en revisión." })))).toMatchObject({
      message: "La solicitud está en revisión. Recarga la página para ver su estado actual.",
    });
  });

  it("sin conexión o bloqueado por CORS, avisa sin romper", async () => {
    const fetchImpl = vi.fn<typeof fetch>().mockRejectedValue(new TypeError("Failed to fetch"));
    expect(await uploadDirect(input(fetchImpl))).toMatchObject({ ok: false, message: expect.stringContaining("conectar") });
  });
});
