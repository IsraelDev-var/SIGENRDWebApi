import { LazyMapView } from "@/features/map";

// Página provisional: comprueba el mapa base. El mapa de disponibilidad completo llega en F3.
export default function HomePage() {
  return (
    <>
      <h1 className="sr-only">Mapa de disponibilidad eléctrica</h1>
      <section aria-label="Mapa de República Dominicana" className="relative min-h-[420px] flex-1">
        {/* Posición absoluta: con solo min-height, el h-full del mapa se resolvería a 0. */}
        <div className="absolute inset-0">
          <LazyMapView />
        </div>
      </section>
    </>
  );
}
