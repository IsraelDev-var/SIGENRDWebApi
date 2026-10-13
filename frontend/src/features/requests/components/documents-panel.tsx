"use client";

import { CircleCheck, CircleDashed, Download, Trash2, Upload } from "lucide-react";
import { useActionState, useId, useState } from "react";

import { FormMessage } from "@/components/shared/form-field";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import type { components } from "@/lib/api/schema";
import { formatDate, formatFileSize } from "@/lib/format";

import { deleteDocumentAction, uploadDocumentAction, type RequestActionState } from "../actions";
import { DOCUMENT_TYPE_LABEL, documentTypeLabel } from "../labels";
import { DOCUMENT_ACCEPT, documentFileError } from "../schemas";
import { controlClass } from "@/components/shared/form-controls";

type Schemas = components["schemas"];

const IDLE: RequestActionState = { status: "idle" };

// El certificado lo genera la distribuidora al aprobar: no se sube.
const UPLOADABLE_TYPES = (Object.keys(DOCUMENT_TYPE_LABEL) as Schemas["DocumentType"][]).filter((t) => t !== "ApprovalCertificate");

type Props = {
  requestId: number;
  required: Schemas["RequiredDocumentsDto"];
  documents: Schemas["DocumentDto"][];
  /** Borrador, Recibida o En corrección (RN-19: después de aprobada nada cambia). */
  canChange: boolean;
};

/** Requisitos del expediente (RN-09, RN-11) y documentos subidos (RN-18). */
export function DocumentsPanel({ requestId, required, documents, canChange }: Props) {
  return (
    <div className="grid gap-6">
      <section aria-labelledby="requisitos" className="grid gap-3">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 id="requisitos" className="text-lg font-semibold">
            Documentos requeridos
          </h2>
          <p className="text-sm text-muted-foreground" role="status">
            {required.items.filter((i) => i.satisfied).length} de {required.items.length} listos
          </p>
        </div>
        <p className="text-sm text-muted-foreground">PDF, JPG o PNG de hasta 10 MB cada uno.</p>
        <ul className="grid gap-3">
          {required.items.map((item) => (
            <li key={item.code} className="grid gap-3 rounded-lg border p-4">
              <div className="flex items-start gap-3">
                {item.satisfied ? (
                  <CircleCheck aria-hidden="true" className="mt-0.5 size-5 shrink-0 text-status-available" />
                ) : (
                  <CircleDashed aria-hidden="true" className="mt-0.5 size-5 shrink-0 text-muted-foreground" />
                )}
                <div className="grid gap-0.5">
                  <p className="font-medium">
                    {item.description}
                    <span className="sr-only">{item.satisfied ? ": listo" : ": pendiente"}</span>
                  </p>
                  {item.acceptedTypes.length > 1 && (
                    <p className="text-sm text-muted-foreground">Sirve cualquiera de: {item.acceptedTypes.map(documentTypeLabel).join(", ")}.</p>
                  )}
                </div>
              </div>
              {canChange && !item.satisfied && <UploadForm requestId={requestId} types={item.acceptedTypes} label={item.description} />}
            </li>
          ))}
        </ul>
        {canChange && (
          <details className="rounded-lg border p-4">
            <summary className="cursor-pointer font-medium">Subir otro documento</summary>
            <div className="mt-3">
              <UploadForm requestId={requestId} types={UPLOADABLE_TYPES} label="otro documento" />
            </div>
          </details>
        )}
      </section>

      <section aria-labelledby="subidos" className="grid gap-3">
        <h2 id="subidos" className="text-lg font-semibold">
          Documentos del expediente
        </h2>
        {documents.length === 0 ? (
          <p className="text-sm text-muted-foreground">Todavía no hay documentos.</p>
        ) : (
          <ul className="grid divide-y rounded-lg border">
            {documents.map((doc) => (
              <li key={doc.id} className="flex flex-wrap items-center justify-between gap-3 p-3">
                <div className="grid min-w-0 gap-0.5">
                  <p className="truncate font-medium">{doc.fileName}</p>
                  <p className="text-sm text-muted-foreground">
                    {documentTypeLabel(doc.documentType)} · {formatFileSize(doc.sizeBytes)} · {formatDate(doc.uploadedAt)}
                  </p>
                </div>
                <div className="flex gap-2">
                  <Button asChild variant="outline" size="sm">
                    <a href={`/portal/solicitudes/${requestId}/documentos/${doc.id}`} download>
                      <Download aria-hidden="true" /> Descargar<span className="sr-only"> {doc.fileName}</span>
                    </a>
                  </Button>
                  {canChange && doc.documentType !== "ApprovalCertificate" && <DeleteForm requestId={requestId} documentId={doc.id} fileName={doc.fileName} />}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function UploadForm({ requestId, types, label }: { requestId: number; types: string[]; label: string }) {
  const [state, action, pending] = useActionState(uploadDocumentAction, IDLE);
  const [fileError, setFileError] = useState<string | null>(null);
  const id = useId();
  const error = fileError ?? state.fieldErrors?.file?.join(" ") ?? null;

  return (
    <form
      action={action}
      noValidate
      className="grid gap-3"
      onSubmit={(e) => {
        // Se revisa en el navegador para no enviar 10 MB que la API rechazaría (CA-06).
        const file = (e.currentTarget.elements.namedItem("file") as HTMLInputElement).files?.[0];
        const problem = documentFileError(file);
        setFileError(problem);
        if (problem) e.preventDefault();
      }}
    >
      <input type="hidden" name="id" value={requestId} />
      <FormMessage status={state.status === "error" && !error ? "error" : state.status === "success" ? "success" : "idle"} message={state.message} />
      <div className="grid gap-3 sm:grid-cols-[minmax(0,14rem)_1fr]">
        {types.length > 1 ? (
          <div className="grid gap-2">
            <Label htmlFor={`${id}-tipo`}>Tipo de documento</Label>
            <select id={`${id}-tipo`} name="documentType" className={`${controlClass} h-8 py-1`} defaultValue={types[0]}>
              {types.map((t) => (
                <option key={t} value={t}>
                  {documentTypeLabel(t)}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <input type="hidden" name="documentType" value={types[0]} />
        )}
        <div className="grid gap-2">
          <Label htmlFor={`${id}-archivo`}>Archivo para {label.toLowerCase()}</Label>
          <input
            id={`${id}-archivo`}
            name="file"
            type="file"
            accept={DOCUMENT_ACCEPT}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? `${id}-error` : undefined}
            onChange={(e) => setFileError(e.target.files?.[0] ? documentFileError(e.target.files[0]) : null)}
            className="text-sm file:mr-3 file:rounded-md file:border file:bg-muted file:px-3 file:py-1 file:text-sm"
          />
          {error && (
            <p id={`${id}-error`} role="alert" className="text-sm text-destructive">
              {error}
            </p>
          )}
        </div>
      </div>
      <Button type="submit" size="sm" variant="secondary" disabled={pending} className="justify-self-start">
        <Upload aria-hidden="true" /> {pending ? "Subiendo…" : "Subir"}
      </Button>
    </form>
  );
}

function DeleteForm({ requestId, documentId, fileName }: { requestId: number; documentId: number; fileName: string }) {
  const [state, action, pending] = useActionState(deleteDocumentAction, IDLE);
  return (
    <form action={action} className="grid gap-1">
      <input type="hidden" name="id" value={requestId} />
      <input type="hidden" name="documentId" value={documentId} />
      <Button type="submit" variant="destructive" size="sm" disabled={pending}>
        <Trash2 aria-hidden="true" /> Quitar<span className="sr-only"> {fileName}</span>
      </Button>
      {state.status === "error" && (
        <p role="alert" className="text-sm text-destructive">
          {state.message}
        </p>
      )}
    </form>
  );
}
