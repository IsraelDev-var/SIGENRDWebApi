import { Suspense } from "react";

import { MapExplorer } from "@/features/map";

// Mapa público de disponibilidad (RF-01 a RF-05). Los filtros y la selección viven en la URL.
export default function HomePage() {
  return (
    <Suspense>
      <MapExplorer />
    </Suspense>
  );
}
