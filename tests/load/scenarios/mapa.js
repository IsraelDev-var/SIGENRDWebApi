// CA-12: 50 usuarios concurrentes consultando el mapa, p95 < 2 s.
import { check, sleep } from 'k6';
import { browseMap, think } from './lib.js';

export const options = {
  scenarios: {
    mapa: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 50 },  // calentamiento
        { duration: '3m', target: 50 },   // 50 usuarios sostenidos
        { duration: '15s', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
  },
  summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

export default function () {
  const res = browseMap();
  check(res, { 'respuesta 200': (r) => r.status === 200 });
  sleep(think(1, 3));
}
