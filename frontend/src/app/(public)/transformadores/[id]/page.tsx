import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { cache } from "react";

import { Button } from "@/components/ui/button";
import { TransformerDetail, type TransformerDetailData } from "@/features/map";
import { ApiError, apiData } from "@/server/api-client";

/** Una sola llamada a la API por petición, aunque la usen la página y sus metadatos. */
const loadTransformer = cache(async (id: string): Promise<TransformerDetailData | null> => {
  if (!/^\d{1,10}$/.test(id)) return null;
  try {
    return await apiData<TransformerDetailData>(`/Transformers/${id}`);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
});

export async function generateMetadata({ params }: PageProps<"/transformadores/[id]">): Promise<Metadata> {
  const transformer = await loadTransformer((await params).id);
  return { title: transformer ? `Transformador ${transformer.code}` : "Transformador no encontrado" };
}

/** Ficha de un transformador con enlace propio, para compartir o consultar sin el mapa (RF-05). */
export default async function TransformerPage({ params }: PageProps<"/transformadores/[id]">) {
  const transformer = await loadTransformer((await params).id);
  if (!transformer) notFound();

  return (
    <div className="mx-auto grid w-full max-w-xl gap-6 px-4 py-6">
      <nav aria-label="Ruta" className="text-sm">
        <Link href="/transformadores" className="text-link underline underline-offset-4 hover:no-underline">
          Transformadores
        </Link>
      </nav>
      <div className="rounded-lg border bg-card p-5">
        <TransformerDetail transformer={transformer} headingLevel={1} />
      </div>
      <Button asChild variant="outline" className="justify-self-start">
        <Link href={`/?t=${transformer.id}`}>Ver en el mapa</Link>
      </Button>
    </div>
  );
}
