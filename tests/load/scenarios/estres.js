// Estrés: tasa creciente de peticiones del mapa (modelo abierto) para hallar cuántas por segundo sostiene
// una instancia de 2 vCPU con p95 < 2 s. Con ese número se estima la cantidad de instancias para RNF-01.
// No falla por umbrales: se busca el punto de quiebre y se lee en el resumen por etapa.
import { check } from 'k6';
import { browseMap } from './lib.js';

const RATES = (__ENV.RATES || '50,100,200,400,800').split(',').map(Number);
const STEP = __ENV.STEP || '1m';

export const options = {
  scenarios: Object.fromEntries(RATES.map((rate, i) => [
    `rps_${rate}`,
    {
      executor: 'constant-arrival-rate',
      rate,
      timeUnit: '1s',
      duration: STEP,
      // Cada etapa empieza cuando termina la anterior (más 10 s de pausa).
      startTime: `${i * (parseInt(STEP, 10) * 60 + 10)}s`,
      preAllocatedVUs: Math.min(rate * 2, 2000),
      maxVUs: Math.min(rate * 6, 4000),
      tags: { etapa: `rps_${rate}` },
    },
  ])),
  thresholds: Object.fromEntries(RATES.flatMap((rate) => [
    // Umbrales por etapa solo para que el resumen muestre p95 y errores de cada una.
    [`http_req_duration{etapa:rps_${rate}}`, ['p(95)<2000']],
    [`http_req_failed{etapa:rps_${rate}}`, ['rate<0.01']],
    [`http_reqs{etapa:rps_${rate}}`, ['count>0']],
  ])),
  summaryTrendStats: ['med', 'p(95)', 'p(99)', 'max'],
};

export default function () {
  const res = browseMap();
  check(res, { 'respuesta 200': (r) => r.status === 200 });
}
