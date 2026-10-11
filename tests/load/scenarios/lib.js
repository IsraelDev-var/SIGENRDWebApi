// Utilidades comunes de los escenarios de carga.
import http from 'k6/http';

export const BASE = __ENV.BASE_URL || 'http://localhost:5299';
const JSON_HEADERS = { 'Content-Type': 'application/json' };

// Centros urbanos de República Dominicana: los encuadres del mapa caen donde están los transformadores.
const CITIES = [
  [18.48, -69.93], [18.49, -69.85], [18.49, -70.0], [19.45, -70.69], [18.42, -70.11],
  [18.43, -68.97], [18.46, -69.31], [19.79, -70.69], [19.22, -70.53], [18.21, -71.1],
];

export const pick = (list) => list[Math.floor(Math.random() * list.length)];
const jitter = (value, spread) => value + (Math.random() - 0.5) * spread;

/** Encuadre del mapa: 'pais' (todo el país), 'ciudad' (~15 km) o 'barrio' (~3 km). */
export function bbox(level) {
  if (level === 'pais') return { minLat: 17.5, maxLat: 19.95, minLng: -71.75, maxLng: -68.3 };
  const [lat, lng] = pick(CITIES);
  const span = level === 'ciudad' ? 0.15 : 0.03;
  const cLat = jitter(lat, 0.05);
  const cLng = jitter(lng, 0.05);
  return { minLat: cLat - span / 2, maxLat: cLat + span / 2, minLng: cLng - span / 2, maxLng: cLng + span / 2 };
}

export function randomPoint() {
  const [lat, lng] = pick(CITIES);
  return { lat: jitter(lat, 0.08), lng: jitter(lng, 0.08) };
}

export function mapUrl(b) {
  return `${BASE}/api/v1/Transformers?MinLatitude=${b.minLat}&MaxLatitude=${b.maxLat}&MinLongitude=${b.minLng}&MaxLongitude=${b.maxLng}`;
}

/** Navegación anónima del mapa (CA-12): encuadres de distintos tamaños, detalle y búsqueda por cercanía. */
export function browseMap() {
  const roll = Math.random();
  if (roll < 0.1) {
    return http.get(mapUrl(bbox('pais')), { tags: { name: 'mapa-pais' } });
  }
  if (roll < 0.6) {
    const res = http.get(mapUrl(bbox('ciudad')), { tags: { name: 'mapa-ciudad' } });
    // A veces el usuario abre el detalle de un transformador del encuadre.
    const items = res.status === 200 ? res.json('data.items') : null;
    if (items && items.length > 0 && Math.random() < 0.3) {
      http.get(`${BASE}/api/v1/Transformers/${pick(items).id}`, { tags: { name: 'transformador-detalle' } });
    }
    return res;
  }
  if (roll < 0.9) {
    return http.get(mapUrl(bbox('barrio')), { tags: { name: 'mapa-barrio' } });
  }
  const p = randomPoint();
  return http.get(`${BASE}/api/v1/Transformers/search?Latitude=${p.lat}&Longitude=${p.lng}&RadiusMeters=2000`,
    { tags: { name: 'transformadores-cercanos' } });
}

export function login() {
  const res = http.post(`${BASE}/api/v1/Auth/login`,
    JSON.stringify({ email: __ENV.ADMIN_EMAIL, password: __ENV.ADMIN_PASSWORD }), { headers: JSON_HEADERS });
  if (res.status !== 200) throw new Error(`login falló: ${res.status} ${res.body}`);
  return res.json('token');
}

export function authHeaders(token, extra = {}) {
  return { headers: Object.assign({ Authorization: `Bearer ${token}` }, extra) };
}

export function jsonAuth(token) {
  return authHeaders(token, JSON_HEADERS);
}

/** Pausa de un usuario real entre acciones. */
export function think(minSeconds = 1, maxSeconds = 3) {
  return minSeconds + Math.random() * (maxSeconds - minSeconds);
}
