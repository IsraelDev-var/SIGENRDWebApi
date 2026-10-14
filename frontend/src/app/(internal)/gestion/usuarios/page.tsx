import type { Metadata } from "next";
import Link from "next/link";

import { PageHeader } from "@/components/shared/page-header";
import { EmptyState, ErrorState } from "@/components/shared/page-states";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { TONE_BADGE_CLASS } from "@/config/status";
import { CreateUserForm, INTERNAL_ROLES, listUsers, roleLabel, UserStatusForm } from "@/features/administration";
import { formatCount, formatDate } from "@/lib/format";
import { ApiError } from "@/server/api-client";
import { requireAction } from "@/server/session";

export const metadata: Metadata = { title: "Usuarios internos" };

const one = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value);

const selectClass =
  "h-9 w-full rounded-md border border-input bg-card px-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring";

/** Usuarios internos (§3.2, B-5): alta, búsqueda, desactivación y reactivación. Solo el administrador. */
export default async function UsersPage({ searchParams }: PageProps<"/gestion/usuarios">) {
  const session = await requireAction("gestion", "/gestion/usuarios", "manageInternalUsers");
  const params = await searchParams;
  const text = one(params.texto)?.trim().slice(0, 100) || undefined;
  const role = (INTERNAL_ROLES as readonly string[]).includes(one(params.rol) ?? "") ? one(params.rol) : undefined;
  const page = Math.max(1, Math.min(10_000, Number(one(params.pagina)) || 1));

  let result: Awaited<ReturnType<typeof listUsers>> | null = null;
  try {
    result = await listUsers(session, { text, role, page });
  } catch (error) {
    if (!(error instanceof ApiError)) throw error;
  }

  const pageHref = (n: number) => {
    const next = new URLSearchParams();
    if (text) next.set("texto", text);
    if (role) next.set("rol", role);
    next.set("pagina", String(n));
    return `/gestion/usuarios?${next}`;
  };

  return (
    <div className="grid grid-cols-1 gap-6">
      <PageHeader title="Usuarios internos" description="Personal de la distribuidora: ingenieros, distribuidora y administradores. Los clientes y las instaladoras se registran solos." />

      <details className="rounded-lg border bg-card p-4">
        <summary className="cursor-pointer font-medium">Nuevo usuario interno</summary>
        <div className="mt-4">
          <CreateUserForm />
        </div>
      </details>

      <form method="get" className="flex flex-wrap items-end gap-3">
        <div className="grid min-w-56 flex-1 gap-1.5">
          <Label htmlFor="u-texto">Nombre o correo</Label>
          <Input id="u-texto" name="texto" defaultValue={text} maxLength={100} />
        </div>
        <div className="grid min-w-44 gap-1.5">
          <Label htmlFor="u-rol">Rol</Label>
          <select id="u-rol" name="rol" defaultValue={role ?? ""} className={selectClass}>
            <option value="">Todos</option>
            {INTERNAL_ROLES.map((r) => (
              <option key={r} value={r}>
                {roleLabel(r)}
              </option>
            ))}
          </select>
        </div>
        <Button type="submit" variant="outline">
          Buscar
        </Button>
      </form>

      {!result ? (
        <ErrorState title="No se pudo cargar la lista" description="La conexión con el servidor falló. Inténtalo de nuevo en unos minutos." />
      ) : result.items.length === 0 ? (
        <EmptyState title="No hay usuarios con esos filtros" />
      ) : (
        <>
          <p role="status" className="text-sm text-muted-foreground">
            {formatCount(result.totalCount, "usuario", "usuarios")}. Página {result.page} de {Math.max(1, result.totalPages)}.
          </p>
          <div className="rounded-lg border bg-card">
            <Table label="Usuarios internos">
              <TableHeader>
                <TableRow>
                  <TableHead scope="col">Nombre</TableHead>
                  <TableHead scope="col">Correo</TableHead>
                  <TableHead scope="col">Rol</TableHead>
                  <TableHead scope="col">Estado</TableHead>
                  <TableHead scope="col">
                    <span className="sr-only">Acciones</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {result.items.map((u) => (
                  <TableRow key={u.id}>
                    <TableCell className="font-medium">{u.fullName || "Sin nombre"}</TableCell>
                    <TableCell>{u.email}</TableCell>
                    <TableCell>{roleLabel(u.role)}</TableCell>
                    <TableCell className="whitespace-normal">
                      <div className="flex flex-wrap gap-1">
                        <Badge className={TONE_BADGE_CLASS[u.isActive ? "success" : "neutral"]}>
                          {u.isActive ? "Activa" : `Desactivada${u.deactivatedAt ? ` el ${formatDate(u.deactivatedAt)}` : ""}`}
                        </Badge>
                        {!u.emailConfirmed && <Badge className={TONE_BADGE_CLASS.warning}>Sin confirmar el correo</Badge>}
                        {u.isLockedOut && <Badge className={TONE_BADGE_CLASS.danger}>Bloqueada por intentos</Badge>}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      {/* La clave reinicia la confirmación cuando cambia el estado. */}
                      <UserStatusForm key={`${u.id}-${u.isActive}`} id={u.id} name={u.fullName || u.email} active={u.isActive} isSelf={u.id === session.userId} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          {result.totalPages > 1 && (
            <nav aria-label="Páginas" className="flex flex-wrap items-center justify-between gap-2">
              {result.page > 1 ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={pageHref(result.page - 1)}>Anterior</Link>
                </Button>
              ) : (
                <span />
              )}
              <span className="text-sm text-muted-foreground">
                Página {result.page} de {result.totalPages}
              </span>
              {result.page < result.totalPages ? (
                <Button asChild variant="outline" size="sm">
                  <Link href={pageHref(result.page + 1)}>Siguiente</Link>
                </Button>
              ) : (
                <span />
              )}
            </nav>
          )}
        </>
      )}
    </div>
  );
}
