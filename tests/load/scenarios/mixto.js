// Carga mixta realista: mapa anónimo, seguimiento público y trabajo autenticado (búsqueda de solicitudes,
// simulaciones y reportes), todos a la vez. Meta: p95 < 2 s en todo (RNF-01) y < 1 s en disponibilidad (RNF-05).
import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE, browseMap, jsonAuth, authHeaders, login, randomPoint, think } from './lib.js';

export const options = {
  scenarios: {
    mixto: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 100 },
        { duration: '3m', target: 100 },
        { duration: '15s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    'http_req_duration{name:mapa-ciudad}': ['p(95)<1000'],      // RNF-05: disponibilidad < 1 s
    'http_req_duration{name:transformadores-cercanos}': ['p(95)<1000'],
    // Un umbral por endpoint para que el resumen muestre los tiempos de cada uno.
    'http_req_duration{name:mapa-pais}': ['p(95)<2000'],
    'http_req_duration{name:mapa-barrio}': ['p(95)<2000'],
    'http_req_duration{name:transformador-detalle}': ['p(95)<2000'],
    'http_req_duration{name:seguimiento}': ['p(95)<2000'],
    'http_req_duration{name:solicitudes-busqueda}': ['p(95)<2000'],
    'http_req_duration{name:simulacion-crear}': ['p(95)<2000'],
    'http_req_duration{name:reporte-estadisticas}': ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
  },
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

export function setup() {
  const token = login();
  // Códigos reales para el seguimiento público.
  const res = http.get(`${BASE}/api/v1/ConnectionRequest/search?pageSize=100`, authHeaders(token));
  const codes = res.json('data.items').map((r) => r.requestCode);
  // Simular es del solicitante (matriz 3.2).
  return { token, codes, customerToken: login('cliente') };
}

export default function (data) {
  const roll = Math.random();
  let res;

  if (roll < 0.6) {
    res = browseMap();
  } else if (roll < 0.7) {
    const code = data.codes[Math.floor(Math.random() * data.codes.length)];
    res = http.get(`${BASE}/api/v1/ConnectionRequest/track/${code}`, { tags: { name: 'seguimiento' } });
  } else if (roll < 0.85) {
    const statuses = ['', '&status=1', '&status=2', '&text=naco'];
    const filter = statuses[Math.floor(Math.random() * statuses.length)];
    res = http.get(`${BASE}/api/v1/ConnectionRequest/search?pageSize=20${filter}`,
      Object.assign(authHeaders(data.token), { tags: { name: 'solicitudes-busqueda' } }));
  } else if (roll < 0.97) {
    const p = randomPoint();
    res = http.post(`${BASE}/api/v1/Simulations`, JSON.stringify({
      monthlyConsumptionKwh: 200 + Math.floor(Math.random() * 800),
      latitude: p.lat,
      longitude: p.lng,
      installedCapacityKw: 1 + Math.floor(Math.random() * 8),
    }), Object.assign(jsonAuth(data.customerToken), { tags: { name: 'simulacion-crear' } }));
  } else {
    res = http.get(`${BASE}/api/v1/Reports/requests/statistics`,
      Object.assign(authHeaders(data.token), { tags: { name: 'reporte-estadisticas' } }));
  }

  check(res, { 'respuesta 2xx': (r) => r.status >= 200 && r.status < 300 });
  sleep(think(1, 3));
}
