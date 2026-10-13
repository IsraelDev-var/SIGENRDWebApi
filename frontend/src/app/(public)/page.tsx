import { LazyMapView } from "@/features/map";

// Página provisional de F0: comprueba el mapa base. El mapa de disponibilidad completo llega en F3.
export default function HomePage() {
  return (
    <main className="flex flex-1 flex-col">
      <header className="border-b px-4 py-3">
        <h1 className="text-lg font-semibold">EnergyMap</h1>
        <p className="text-sm text-muted-foreground">
          Vista previa técnica. Los datos de los transformadores todavía no se muestran.
        </p>
      </header>
      <section aria-label="Mapa de República Dominicana" className="relative min-h-[420px] flex-1">
        {/* Posición absoluta: con solo min-height, el h-full del mapa se resolvería a 0. */}
        <div className="absolute inset-0">
          <LazyMapView />
        </div>
      </section>
    </main>
  );
}
