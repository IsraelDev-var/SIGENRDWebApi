// @vitest-environment node
import { exportSPKI, generateKeyPair, SignJWT, type CryptoKey } from "jose";
import { beforeAll, describe, expect, it } from "vitest";

import { verifyAccessToken, type TokenSettings } from "@/server/token";

const ISSUER = "SIGENRD.API";
const AUDIENCE = "SIGENRD.Client";
const ROLE_URI = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

let privateKey: CryptoKey;
let otherPrivateKey: CryptoKey;
let settings: TokenSettings;

beforeAll(async () => {
  const pair = await generateKeyPair("RS256", { extractable: true });
  privateKey = pair.privateKey;
  otherPrivateKey = (await generateKeyPair("RS256")).privateKey;
  settings = { publicKeyPem: await exportSPKI(pair.publicKey), issuer: ISSUER, audience: AUDIENCE };
});

type Overrides = { claims?: Record<string, unknown>; issuer?: string; audience?: string; expiresIn?: string | number; key?: CryptoKey };

// Igual que JwtTokenGenerator de la API: sub, email, FullName, UserType, jti y el rol.
function token({ claims = { role: "Engineer" }, issuer = ISSUER, audience = AUDIENCE, expiresIn = "60m", key }: Overrides = {}) {
  return new SignJWT({ email: "ingeniero@test.energymap.local", FullName: "Ingeniero de prueba", UserType: "Engineer", ...claims })
    .setProtectedHeader({ alg: "RS256", typ: "JWT" })
    .setSubject("user-123")
    .setJti("jti-1")
    .setIssuer(issuer)
    .setAudience(audience)
    .setExpirationTime(expiresIn)
    .sign(key ?? privateKey);
}

describe("verificación del token de la API", () => {
  it("devuelve el usuario de un token válido", async () => {
    const user = await verifyAccessToken(await token(), settings);

    expect(user).toMatchObject({
      userId: "user-123",
      email: "ingeniero@test.energymap.local",
      fullName: "Ingeniero de prueba",
      role: "Engineer",
    });
    expect(user!.expiresAt).toBeGreaterThan(Date.now());
  });

  it("lee el rol con el nombre largo de .NET (ClaimTypes.Role)", async () => {
    const user = await verifyAccessToken(await token({ claims: { [ROLE_URI]: "Distributor" } }), settings);
    expect(user?.role).toBe("Distributor");
  });

  it("lee la EDE del personal (distributor_id) y por defecto no tiene", async () => {
    expect((await verifyAccessToken(await token(), settings))?.distributorId).toBeNull();
    expect((await verifyAccessToken(await token({ claims: { role: "Engineer", distributor_id: "3" } }), settings))?.distributorId).toBe(3);
    expect((await verifyAccessToken(await token({ claims: { role: "Engineer", distributor_id: "x" } }), settings))?.distributorId).toBeNull();
  });

  it("lee la marca de contraseña temporal (pwd_change) y por defecto no la tiene", async () => {
    expect((await verifyAccessToken(await token(), settings))?.mustChangePassword).toBe(false);
    expect((await verifyAccessToken(await token({ claims: { role: "Engineer", pwd_change: "true" } }), settings))?.mustChangePassword).toBe(true);
  });

  it("acepta el rol como lista", async () => {
    const user = await verifyAccessToken(await token({ claims: { role: ["Admin"] } }), settings);
    expect(user?.role).toBe("Admin");
  });

  it.each([
    ["firmado con otra llave", { key: undefined as CryptoKey | undefined, other: true }],
    ["de otro emisor", { issuer: "OTRA.API" }],
    ["para otra audiencia", { audience: "Otro.Cliente" }],
    ["vencido", { expiresIn: Math.floor(Date.now() / 1000) - 60 }],
    ["sin un rol conocido", { claims: { role: "SuperUser" } }],
  ])("rechaza un token %s", async (_name, overrides) => {
    const { other, ...rest } = overrides as Overrides & { other?: boolean };
    const value = await token({ ...rest, key: other ? otherPrivateKey : undefined });
    expect(await verifyAccessToken(value, settings)).toBeNull();
  });

  it("rechaza un token sin firma (alg none)", async () => {
    const [, payload] = (await token()).split(".");
    const unsigned = `${Buffer.from(JSON.stringify({ alg: "none", typ: "JWT" })).toString("base64url")}.${payload}.`;
    expect(await verifyAccessToken(unsigned, settings)).toBeNull();
  });

  it("rechaza basura y la ausencia de token", async () => {
    expect(await verifyAccessToken("no-es-un-jwt", settings)).toBeNull();
    expect(await verifyAccessToken(undefined, settings)).toBeNull();
  });
});
