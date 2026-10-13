import { describe, expect, it } from "vitest";

import { clientIpFrom } from "@/server/client-ip";

describe("IP del usuario desde X-Forwarded-For", () => {
  it("con un proxy de confianza toma la última entrada, la que agregó la plataforma", () => {
    // El usuario intentó falsificar su IP con 1.2.3.4; Azure agregó la real al final.
    expect(clientIpFrom("1.2.3.4, 203.0.113.7", 1)).toBe("203.0.113.7");
  });

  it("con dos proxies de confianza toma la penúltima", () => {
    expect(clientIpFrom("1.2.3.4, 203.0.113.7, 10.0.0.5", 2)).toBe("203.0.113.7");
  });

  it("quita el puerto que agrega Azure", () => {
    expect(clientIpFrom("203.0.113.7:51234", 1)).toBe("203.0.113.7");
    expect(clientIpFrom("[2001:db8::1]:443", 1)).toBe("2001:db8::1");
  });

  it("convierte la IPv4 dentro de IPv6 que reporta Node", () => {
    expect(clientIpFrom("::ffff:127.0.0.1", 1)).toBe("127.0.0.1");
  });

  it("acepta IPv6 sin corchetes", () => {
    expect(clientIpFrom("2001:db8::1", 1)).toBe("2001:db8::1");
  });

  it.each([null, "", "  ", "no-es-ip", "999.1.1.1", "203.0.113.7; DROP TABLE"])("descarta %s", (value) => {
    expect(clientIpFrom(value, 1)).toBeNull();
  });

  it("si hay menos entradas que proxies configurados no inventa una IP", () => {
    expect(clientIpFrom("203.0.113.7", 3)).toBeNull();
  });
});
