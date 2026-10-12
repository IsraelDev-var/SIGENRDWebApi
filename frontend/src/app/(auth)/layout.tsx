import { Brand, SkipLink } from "@/components/layout/brand";

/** Páginas de acceso: una tarjeta centrada, sin el menú de la plataforma. */
export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-full flex-1 flex-col items-center gap-6 px-4 py-10">
      <SkipLink />
      <Brand className="text-lg" />
      <main id="contenido" className="w-full max-w-md rounded-xl border bg-card p-6 shadow-sm sm:p-8">
        {children}
      </main>
    </div>
  );
}
