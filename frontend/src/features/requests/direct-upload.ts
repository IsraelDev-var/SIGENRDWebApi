/**
 * Subida directa de un documento del navegador a la API, con el permiso de un solo uso que pidió el servidor
 * (requestUploadTicketAction). Así el archivo de hasta 10 MB (RN-18) no pasa por el servidor de Next, que en Vercel
 * no acepta cuerpos de más de 4,5 MB.
 */

export type DirectUploadResult = { ok: true } | { ok: false; message: string };

type Input = {
  uploadUrl: string;
  ticket: string;
  file: File;
  documentType: string;
  fetchImpl?: typeof fetch;
};

const NETWORK_ERROR = "No pudimos conectar con el servidor para subir el archivo. Revisa tu conexión e inténtalo de nuevo.";

export async function uploadDirect({ uploadUrl, ticket, file, documentType, fetchImpl = fetch }: Input): Promise<DirectUploadResult> {
  const body = new FormData();
  body.set("File", file);
  body.set("DocumentType", documentType);

  let response: Response;
  try {
    response = await fetchImpl(uploadUrl, {
      method: "POST",
      headers: { Authorization: `Bearer ${ticket}` },
      body,
      // El permiso va en la cabecera; ninguna cookie del sitio viaja a la API.
      credentials: "omit",
    });
  } catch {
    return { ok: false, message: NETWORK_ERROR };
  }

  if (response.ok) return { ok: true };
  return { ok: false, message: await errorMessage(response) };
}

async function errorMessage(response: Response): Promise<string> {
  switch (response.status) {
    // Permiso vencido o ya usado: el formulario pide otro en el siguiente intento.
    case 401:
      return "La subida tardó demasiado y el permiso venció. Inténtalo de nuevo.";
    case 404:
      return "La solicitud no existe o no es tuya.";
    case 413:
      return "El archivo pasa de 10 MB. Redúcelo o divídelo e inténtalo de nuevo.";
  }

  // El resto trae el sobre Response<T> de la API con el mensaje pensado para el usuario.
  let envelope: { message?: string | null; errors?: string[] | null } | null = null;
  try {
    envelope = await response.json();
  } catch {
    // Sin cuerpo JSON: mensaje genérico.
  }
  const detail = envelope?.errors?.length ? envelope.errors.join(" ") : envelope?.message;
  if (response.status === 409) return `${detail ?? "La solicitud ya no admite cambios en los documentos."} Recarga la página para ver su estado actual.`;
  return detail ?? "No pudimos subir el archivo. Inténtalo de nuevo en unos minutos.";
}
