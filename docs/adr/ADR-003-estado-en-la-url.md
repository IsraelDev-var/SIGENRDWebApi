# ADR-003: estado compartido en la URL, sin almacén global

**Estado:** aceptada (2026-10-11) · **Fases:** F3 a F8

## Contexto

Filtros del mapa, transformador elegido, filtros de bandejas y listas, período de reportes y páginas se comparten por enlace y deben sobrevivir al botón Atrás.

## Decisión

- El estado que importa va en la URL: `nuqs` en el mapa (`?estado=&zona=&disp=&t=`) y formularios GET en las listas (`?estado=&pagina=`, `?desde=&hasta=`), que además funcionan sin JavaScript.
- TanStack Query solo donde el navegador pide datos: el mapa por área visible y el conteo de notificaciones.
- Formularios de varios pasos (asistente de solicitud, simulador) con estado local del componente.
- Sin Redux ni Zustand.

## Consecuencias

- Las páginas de servidor leen `searchParams` y validan cada parámetro (nada inválido llega a la API).
- Si aparece estado que no cabe en la URL ni en un componente, se reconsidera.
