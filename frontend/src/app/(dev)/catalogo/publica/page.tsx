import { PublicShell } from "@/components/layout/shells";
import { StatusLegend } from "@/components/shared/status-legend";
import { Button } from "@/components/ui/button";
import { PUBLIC_NAV } from "@/config/navigation";

export default function PublicShellDemo() {
  return (
    <PublicShell nav={PUBLIC_NAV} activeHref="/" actions={<Button size="sm">Ingresar</Button>}>
      <div className="grid flex-1 gap-4 p-4 md:grid-cols-[1fr_18rem]">
        <div className="flex min-h-80 items-center justify-center rounded-lg border border-dashed text-muted-foreground">
          <h1 className="text-base font-normal">Zona pública: aquí va el mapa</h1>
        </div>
        <aside aria-label="Leyenda" className="rounded-lg border bg-card p-4">
          <StatusLegend />
        </aside>
      </div>
    </PublicShell>
  );
}
