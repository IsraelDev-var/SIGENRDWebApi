# Frontend: Design System

Detalle técnico en la sección "Design System" de [`frontend/README.md`](../../frontend/README.md). Resumen de las reglas:

- **Identidad:** dirección B de la propuesta (mapa al centro), con la densidad de la dirección C en la gestión (tablas, barra lateral). Sin la marca de Edesur.
- **Tipografía:** Public Sans en la interfaz; IBM Plex Mono para códigos, kVA y montos (cifras tabulares).
- **Color:** tokens en `globals.css`, tema claro y oscuro según el sistema (D-6). El contraste WCAG 2.1 AA de cada par se prueba en `design-tokens.test.ts`.
- **Semáforo (RN-02):** el frontend no calcula estados: traduce el de la API a etiqueta, color, forma e icono (`config/status.ts`). Crítica y Saturada comparten el rojo pero no la forma ni el icono. En mantenimiento la disponibilidad "no se calcula".
- **Estados de página:** vacío, error y sin acceso (`components/shared/page-states.tsx`); mensajes de formulario con `role="alert"` o `"status"`.
- **Formularios:** `FormField`, `SelectField`, `TextareaField`, `ChoiceCards` asocian etiqueta, ayuda y error (WCAG 1.3.1, 3.3.1). Tras un error conservan lo escrito (salvo contraseñas).
- **Tablas:** contenedor con desplazamiento enfocable con el teclado; las páginas usan columnas `minmax(0, 1fr)` para no desplazarse en horizontal a 360 px.
- **Gráficos:** siempre con su tabla equivalente y sin animación.
- **Catálogo:** `/catalogo` con `ENERGYMAP_DESIGN_CATALOG=true`.
