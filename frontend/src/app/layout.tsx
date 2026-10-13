import type { Metadata } from "next";
import { IBM_Plex_Mono, Public_Sans } from "next/font/google";

import { TooltipProvider } from "@/components/ui/tooltip";

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

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es-DO" className={`${publicSans.variable} ${plexMono.variable} h-full antialiased`}>
      <body className="flex min-h-full flex-col">
        {/* Requerido por los tooltips de Radix en cualquier página. */}
        <TooltipProvider>{children}</TooltipProvider>
      </body>
    </html>
  );
}
