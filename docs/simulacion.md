# Simulación de medición neta

Módulo informativo (RF-18 a RF-26): estima producción, ahorro, ROI y retorno de un sistema fotovoltaico, lo clasifica y da recomendaciones. **No es una solicitud, no tiene valor regulatorio y no reserva capacidad del transformador.**

## Endpoints

| Método y ruta | Para qué |
|---|---|
| `POST /api/v1/Simulations` | Crear una simulación (RF-18 a RF-21, RF-23 a RF-25) |
| `GET /api/v1/Simulations` | Historial propio, paginado (RF-26) |
| `GET /api/v1/Simulations/{id}` | Detalle con parámetros, supuestos y resultados |
| `GET /api/v1/Simulations/compare?ids=1&ids=2` | Comparar de 2 a 5 simulaciones propias (RF-22) |
| `GET /api/v1/Simulations/{id}/report` | Resumen en PDF (CA-10) |

Roles: Cliente, Instaladora y Administrador. Cada usuario ve solo sus simulaciones; una ajena responde 404.

Datos de entrada: consumo mensual (kWh), ubicación (dentro de República Dominicana) y potencia estimada (kWp) **o** los paneles e inversores (RF-24). Opcionales: costo de la cotización y precio fijo por kWh para tarifas distintas de BTS-1.

## Fórmula v1

La fórmula es una pieza intercambiable (patrón Strategy, `INetMeteringFormula`): si la SIE o Edesur validan otra, se agrega una versión nueva sin tocar el resto. Cada simulación guarda la versión y los supuestos con que se calculó.

1. **Producción anual** = kWp × rendimiento específico (kWh por kWp al año). Se reparte igual en los 12 meses.
2. **Medición neta mes a mes**: lo producido cubre el consumo del mes; el sobrante pasa como crédito en kWh al mes siguiente. Si falta energía, se compra a la red con la tarifa.
3. **Pago del excedente**: el crédito que queda a diciembre se paga al 75 % del precio del primer tramo de BTS-1.
4. **Ahorro anual** = cargo por energía sin paneles − cargo por energía con paneles + pago del excedente. El cargo fijo no cuenta: se paga igual con o sin paneles.
5. **Retorno simple** (años) = inversión ÷ ahorro anual. Sin ahorro no hay retorno.
6. **ROI** (% en la vida útil) = (suma del ahorro de cada año, reducido por la degradación anual − inversión) ÷ inversión × 100.
7. **Clasificación** por años de retorno: Rentable, Condicionalmente rentable o No rentable.

Si el usuario no indica la inversión, se estima como kWp × costo por kWp. Si registra paneles, su potencia total reemplaza a la estimada.

### Tarifa BTS-1

Escalonada; **desde 701 kWh en el mes, todo el consumo se cobra al precio del último tramo**.

| Tramo | RD$/kWh (Edesur) |
|---|---|
| 0–200 kWh | 6.05 |
| 201–300 kWh | 8.59 |
| 301–700 kWh | 12.89 |
| 701 kWh o más (todo el consumo) | 13.09 |

Verificada contra los totales publicados para el mismo período: 700 kWh = RD$7,353.59 y 701 kWh = RD$9,304.68, con un cargo fijo de RD$128.59 (hay una prueba automática que lo comprueba).

## Supuestos

Todos son configurables en la sección `Simulation` de la configuración (por ejemplo, `Simulation__CostPerKwp`).

| Supuesto | Valor | Fuente | Estado |
|---|---|---|---|
| Tarifa | La de la tabla de arriba | Resolución SIE-079-2026-TF, tarifas de transición jul-sep 2026, según [El Caribe](https://www.elcaribe.com.do/panorama/dinero/cuanto-paga-kwh-factura-electrica-rd/) | **Actualizar cada trimestre** con la resolución vigente en [sie.gob.do](https://sie.gob.do) |
| Pago del excedente | 75 % del primer tramo BTS-1, a diciembre | [Reglamento de medición neta (CNE)](https://www.cne.gob.do/wp-content/uploads/2016/06/Reglamento.Medicion.Neta_.pdf), primera edición | Confirmar que sigue vigente |
| Rendimiento específico | 1,500 kWh/kWp/año | Producciones esperadas de proyectos reales en Santo Domingo (≈1,490–1,550), [pv magazine / Soventix](https://www.pv-magazine.com/press-releases/soventix-further-expands-its-position-as-a-leading-project-developer-for-solar-power-plants-in-the-caribbean/) | Conservador; ya incluye pérdidas |
| Degradación anual | 0.5 % | Valor habitual de garantías de fabricantes | Provisional |
| Vida útil | 25 años | Garantía de producción habitual | Provisional |
| Costo por kWp | RD$60,000 | Estimación | **Provisional**: confirmar con instaladoras validadas |
| Rentable | Retorno ≤ 6 años | Criterio del proyecto | **Provisional** |
| Condicionalmente rentable | Retorno ≤ 10 años | Criterio del proyecto | **Provisional** |
| Factor de potencia | El de la distribuidora (0.9 por defecto) | Igual que en el resto del sistema | Provisional |
| Radio de transformadores de referencia | 2 km, hasta 5 | Criterio del proyecto | — |

Para tarifas comerciales o industriales, el usuario indica un precio fijo por kWh.

## Recomendaciones (RF-25)

Reglas simples y explicables:

- Cobertura mayor al 110 %: sugiere reducir potencia, porque el excedente se paga a menos de lo que se ahorra consumiéndolo.
- Cobertura menor al 60 % con resultado rentable: sugiere ampliar hasta cubrir cerca del 90 %.
- Si con paneles el consumo de la red queda apenas por encima de 701 kWh: sugiere cuántos kWp agregar para bajar de tramo.
- Retorno largo o sin ahorro: sugiere revisar la cotización o los datos.
- Transformador cercano saturado, en mantenimiento, crítico o sin los kVA necesarios: lo advierte. Sin transformadores en el radio, también.
- Paneles e inversores desproporcionados (relación fuera de 0.8 a 1.35): lo advierte.

## Limitaciones

- Producción plana en el año: no modela la variación por estación ni la inclinación u orientación de los paneles.
- El ahorro se reduce con la degradación en la misma proporción que la producción (aproximación).
- Sin escalamiento de tarifas ni costo financiero.
- CA-09 pide exactitud frente a una fórmula validada por la SIE o la distribuidora: la v1 está documentada para esa validación, pero todavía no fue contrastada con su hoja de cálculo.
