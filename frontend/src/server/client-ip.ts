/**
 * IP del usuario a partir de X-Forwarded-For, para reenviarla a la API (que limita peticiones por IP).
 *
 * Next solo escribe X-Forwarded-For si no viene en la petición, así que el usuario puede mandar el suyo.
 * Lo confiable es lo que agregó el proxy de la plataforma (Azure App Service lo pone al final). Con N proxies
 * de confianza delante de Next, la IP del usuario es la N-ésima entrada contando desde el final.
 */
export function clientIpFrom(forwardedFor: string | null | undefined, trustedProxies: number): string | null {
  if (!forwardedFor) return null;

  const entries = forwardedFor
    .split(",")
    .map((entry) => entry.trim())
    .filter(Boolean);

  const index = entries.length - Math.max(1, trustedProxies);
  const raw = entries[index];
  return raw ? normalize(raw) : null;
}

/** Quita el puerto ("203.0.113.5:4431", "[2001:db8::1]:443") y descarta lo que no es una IP. */
function normalize(entry: string): string | null {
  let value = entry;
  const bracketed = /^\[([^\]]+)\](?::\d+)?$/.exec(value);
  if (bracketed) value = bracketed[1]!;
  else if (/^[\d.]+:\d+$/.test(value)) value = value.slice(0, value.lastIndexOf(":"));

  const ipv4 = /^(25[0-5]|2[0-4]\d|1?\d?\d)(\.(25[0-5]|2[0-4]\d|1?\d?\d)){3}$/;
  // IPv4 dentro de IPv6, como la reporta Node: "::ffff:127.0.0.1".
  const mapped = /^::ffff:(.+)$/i.exec(value);
  if (mapped && ipv4.test(mapped[1]!)) return mapped[1]!;
  const ipv6 = /^[0-9a-f:]+(%\w+)?$/i;
  return ipv4.test(value) || (value.includes(":") && ipv6.test(value)) ? value : null;
}
