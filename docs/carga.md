# Pruebas de carga (fase 7)

Resultados del 2026-10-11 con k6 sobre la **imagen Docker de producción** de la API. Se repiten con:

```bash
tests/load/run.sh mapa mixto documentos estres
```

## Entorno

| Pieza | Configuración |
|---|---|
| API | Imagen de producción (`Dockerfile`), `ASPNETCORE_ENVIRONMENT=Production`, **limitada a 2 vCPU y 2 GB** (como un plan pequeño de Azure App Service) |
| Base de datos | PostGIS 17 en contenedor, **sin límite de CPU** (en la máquina de pruebas: 16 núcleos lógicos) |
| Documentos | Azurite (emulador de Azure Blob Storage) |
| Datos | 10.000 transformadores (RNF-02) en 12 zonas, 20.000 solicitudes del último año, 2.000 clientes, 50 instaladoras (`tests/load/seed.sql`) |
| Generador | k6 1.8.1 en la misma máquina |

Límites del entorno, a tener en cuenta al leer los números:

- k6, la API y la base comparten la misma máquina (Docker Desktop sobre Windows). No hay latencia de red real entre cliente, API y base.
- La base no tiene límite de CPU: en Azure, un servidor pequeño de PostgreSQL llegaría antes a su tope (ver más abajo).
- Toda la carga sale de una IP, así que el límite de peticiones por IP se sube en esta prueba.

## Resultados

### CA-12: 50 usuarios concurrentes en el mapa

50 usuarios durante 3 minutos. Cada uno pide un encuadre del mapa (país, ciudad o barrio), a veces abre el detalle de un transformador o busca por cercanía, y espera de 1 a 3 s entre acciones.

| Métrica | Resultado | Criterio |
|---|---|---|
| Peticiones | 5.844 | — |
| Mediana | 6.6 ms | — |
| **p95** | **22.7 ms** | < 2 s ✅ |
| p99 | 34.4 ms | — |
| Errores | 0 % | < 1 % ✅ |

**CA-12 se cumple** con amplio margen.

### Carga mixta: 100 usuarios

Mapa anónimo (60 %), seguimiento público (10 %), búsqueda de solicitudes (15 %), creación de simulaciones (12 %) y estadísticas de reportes (3 %), con pausas de 1 a 3 s. 10.992 peticiones, **0 % de errores**.

| Endpoint | Mediana | p95 | p99 |
|---|---|---|---|
| Mapa: país (hasta 2.000 transformadores) | 11.0 ms | 33.2 ms | 43.8 ms |
| Mapa: ciudad | 7.2 ms | 24.9 ms | 44.3 ms |
| Mapa: barrio | 2.9 ms | 8.3 ms | 17.5 ms |
| Detalle de transformador | 1.7 ms | 3.9 ms | 7.2 ms |
| Transformadores cercanos | 2.9 ms | 9.8 ms | 20.6 ms |
| Seguimiento público | 2.0 ms | 6.9 ms | 17.0 ms |
| Búsqueda de solicitudes | 4.3 ms | 13.3 ms | 26.8 ms |
| Crear simulación | 8.4 ms | 22.8 ms | 39.1 ms |
| Estadísticas de reportes (20.000 solicitudes) | 110.3 ms | 153.3 ms | 169.7 ms |

CPU máxima: API 55 % de un núcleo (de 2 disponibles), base 63 % de un núcleo.

- **RNF-01** (p95 < 2 s): ✅ en todos los endpoints.
- **RNF-05** (disponibilidad < 1 s): ✅ el mapa por ciudad y la búsqueda por cercanía responden en p95 25 ms y 10 ms.

### RNF-04: documentos de 10 MB

40 subidas de un PDF de 10 MB, 5 a la vez, contra Azurite.

| Métrica | Resultado | Criterio |
|---|---|---|
| Mediana | 403 ms | — |
| **p95** | **1.06 s** | < 5 s ✅ |
| Máximo | 1.06 s | — |
| Errores | 0 % | — |

Con Azure Blob Storage real hay que sumar la latencia de red hasta el almacenamiento; el margen hasta 5 s es amplio.

### Estrés: límite de una instancia

Tráfico de mapa a tasa fija (modelo abierto), en etapas de 1 minuto.

| Tasa pedida | Atendidas | Mediana | p95 | Errores | CPU API (máx. de 200 %) | CPU base |
|---|---|---|---|---|---|---|
| 50 /s | 3.456 | 4.5 ms | 18 ms | 0 % | ~27 % | ~30 % |
| 100 /s | 6.866 | 4.1 ms | 16 ms | 0 % | ~42 % | ~47 % |
| 200 /s | 13.792 | 3.9 ms | 15 ms | 0 % | ~80 % | ~88 % |
| 400 /s | 27.583 | 7.9 ms | **0.69–0.96 s** | 0 % | ~170 % | ~200 % |
| 800 /s | ~600 /s efectivas | 7.2 s | **más de 9 s** | 0 % | **205 % (saturada)** | ~470 % |

(La etapa de 400 /s se midió dos veces: p95 de 685 ms y de 957 ms.)

**Una instancia de 2 vCPU sostiene unas 400 peticiones de mapa por segundo con p95 < 2 s.** A partir de ahí se satura su CPU. No hubo errores en ninguna etapa: con sobrecarga, la API responde lento pero no falla.

## Con la caché del mapa

Después de las mediciones de arriba se agregó una caché del mapa (ver [despliegue.md](despliegue.md), `MapCache`): todos los transformadores se guardan en memoria y los encuadres y las búsquedas por cercanía se resuelven sin consultar la base. La copia se descarta al confirmar cualquier cambio en un transformador y vence a los 30 s (cubre a las demás instancias). El mapa responde además con `Cache-Control: public, max-age=30`. Mismo entorno y mismos escenarios:

| Escenario | Sin caché | Con caché |
|---|---|---|
| Mapa, 50 usuarios: p95 | 22.7 ms | **9.8 ms** (CPU de la base: 8 % de un núcleo) |
| Mixto: mapa del país, p95 | 33.2 ms | **14.5 ms** |
| Mixto: mapa por ciudad, p95 | 24.9 ms | **12.5 ms** |
| Mixto: cercanía, p95 | 9.8 ms | **6.4 ms** |
| Estrés 400 /s: p95 | 0.69–0.96 s | **3.5 ms** (base ~6 %, antes ~200 %) |
| Estrés 800 /s: p95 | más de 9 s (saturada) | **7.7 ms**, 0 % errores (base 6–12 %) |
| Estrés 1.200 /s | — | Saturada: p95 17.6 s, 3 % de errores por tiempo de espera |

Con caché, **una instancia de 2 vCPU sostiene unas 800 peticiones de mapa por segundo** (el doble) y la base deja de trabajar para el mapa. En la etapa saturada de 1.200 /s se vio un pico breve de CPU de la base y una lectura de CPU de la API imposible (más de 2 vCPU), así que esas muestras no son fiables; en régimen normal la base apenas participa.

## Qué significa para RNF-01 (5.000 usuarios)

Con el ritmo del escenario del mapa (una petición cada ~2 s por usuario), 800 peticiones por segundo equivalen a **unos 1.400 usuarios concurrentes por instancia** de 2 vCPU.

- **Piloto (CA-12, 50 usuarios):** una instancia sobra.
- **Meta de diseño (5.000 usuarios, ~2.900 peticiones/s):** unas **4 instancias** de 2 vCPU, con margen 5. La API no guarda estado y escala horizontalmente; con la caché, el mapa ya no carga la base, que deja de ser el cuello de botella.

Antes de la caché hacían falta unas 8 instancias y la base necesitaba unos 14 núcleos solo para el mapa.

Mejoras posibles, si hicieran falta:

1. **Agrupamiento (clustering) en el servidor** para la vista del país (RNF-23): en lugar de 2.000 puntos se envían unos cientos de grupos. Reduce CPU de serialización y tamaño de respuesta.
2. **CDN** delante de la API: el mapa ya sale con `Cache-Control: public`.

## Pendiente

- Repetir estas pruebas en Azure, con la red, la base y el almacenamiento reales, antes de producción.
