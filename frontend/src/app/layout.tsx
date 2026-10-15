import type { Metadata } from "next";
import { IBM_Plex_Mono, Public_Sans } from "next/font/google";
import { connection } from "next/server";

import { TooltipProvider } from "@/components/ui/tooltip";

import { Providers } from "./providers";

import "./globals.css";

const publicSans = Public_Sans({
  variable: "--font-sans",
  subsets: ["latin"],
});

// Códigos de transformador, kVA y montos (cifras tabulares).
const plexMono = IBM_Plex_Mono({
  variable: "--font-plex-mono",
  subsets: ["latin"],
  weight: ["400", "500"],
});

export const metadata: Metadata = {
  title: {
    default: "EnergyMap",
    template: "%s · EnergyMap",
  },
  description: "Disponibilidad eléctrica de los transformadores de distribución en República Dominicana.",
};

export default async function RootLayout({ children }: LayoutProps<"/">) {
  // Todas las páginas se renderizan por petición: la Content Security Policy lleva un nonce nuevo cada vez (proxy.ts) y
  // una página prerenderizada no lo tendría en sus scripts.
  await connection();

  return (
    <html lang="es-DO" className={`${publicSans.variable} ${plexMono.variable} h-full antialiased`}>
      <body className="flex min-h-full flex-col">
        {/* Requerido por los tooltips de Radix en cualquier página. */}
        <Providers>
          <TooltipProvider>{children}</TooltipProvider>
        </Providers>
      </body>
    </html>
  );
}
