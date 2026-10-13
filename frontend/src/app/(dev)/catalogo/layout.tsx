import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { connection } from "next/server";

export const metadata: Metadata = {
  title: "Catálogo de componentes",
  robots: { index: false, follow: false },
};

/**
 * Catálogo interno del Design System. Solo existe con ENERGYMAP_DESIGN_CATALOG=true (desarrollo y pruebas E2E);
 * en cualquier otro caso responde 404. Se lee en cada petición, no al compilar.
 */
export default async function CatalogLayout({ children }: LayoutProps<"/catalogo">) {
  await connection();
  if (process.env.ENERGYMAP_DESIGN_CATALOG !== "true") notFound();

  return children;
}
