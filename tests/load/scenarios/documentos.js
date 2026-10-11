// RNF-04: la carga de un documento de 10 MB (validación + Blob Storage) termina en menos de 5 s.
import http from 'k6/http';
import { check } from 'k6';
import { BASE, authHeaders, login } from './lib.js';

// Un PDF válido de 10 MB (la API rechaza los de más de 10 MB): cabecera, relleno y cierre.
const SIZE = 10 * 1024 * 1024;
const HEADER = '%PDF-1.4\n% EnergyMap carga\n';
const FOOTER = '\n%%EOF\n';
const PDF = HEADER + 'x'.repeat(SIZE - HEADER.length - FOOTER.length) + FOOTER;

export const options = {
  scenarios: {
    documentos: { executor: 'shared-iterations', vus: 5, iterations: 40, maxDuration: '5m' },
  },
  thresholds: {
    'http_req_duration{name:documento-subir}': ['p(95)<5000', 'max<8000'],
    http_req_failed: ['rate<0.01'],
  },
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

export function setup() {
  const token = login();
  // Solicitudes Recibidas: las únicas que aceptan documentos nuevos.
  const res = http.get(`${BASE}/api/v1/ConnectionRequest/search?status=1&pageSize=100`, authHeaders(token));
  const ids = res.json('data.items').map((r) => r.id);
  if (ids.length === 0) throw new Error('no hay solicitudes Recibidas');
  return { token, ids };
}

export default function (data) {
  const id = data.ids[Math.floor(Math.random() * data.ids.length)];
  const body = { file: http.file(PDF, 'plano-10mb.pdf', 'application/pdf'), documentType: '1' };

  const res = http.post(`${BASE}/api/v1/ConnectionRequest/${id}/documents`, body,
    Object.assign(authHeaders(data.token), { tags: { name: 'documento-subir' }, timeout: '60s' }));

  check(res, {
    'subida 200': (r) => r.status === 200,
    'menos de 5 s': (r) => r.timings.duration < 5000,
  });
}
