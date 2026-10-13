import { asRequestStatus, REQUEST_STATUS } from "@/config/status";
import type { components } from "@/lib/api/schema";
import { formatDateTime } from "@/lib/format";

/** Historial de estados del expediente, del más reciente al más antiguo (RN-10). */
export function RequestTimeline({ history }: { history: components["schemas"]["StateHistoryDto"][] }) {
  const entries = [...history].reverse();

  return (
    <section aria-labelledby="historial" className="grid gap-3">
      <h2 id="historial" className="text-lg font-semibold">
        Historial
      </h2>
      <ol className="grid gap-4 border-l pl-4">
        {entries.map((entry) => {
          const status = asRequestStatus(entry.newState);
          return (
            <li key={entry.id} className="relative grid gap-0.5">
              <span aria-hidden="true" className="absolute top-1.5 -left-[1.3rem] size-2.5 rounded-full border-2 border-background bg-primary" />
              <p className="font-medium">{status ? REQUEST_STATUS[status].label : (entry.newState ?? "Cambio")}</p>
              <p className="text-sm text-muted-foreground">
                <time dateTime={entry.createdAt}>{formatDateTime(entry.createdAt)}</time>
              </p>
              {entry.comment && <p className="text-sm">{entry.comment}</p>}
            </li>
          );
        })}
      </ol>
    </section>
  );
}
