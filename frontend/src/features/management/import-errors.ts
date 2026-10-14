/**
 * Errores de la importación CSV tal como los devuelve la API ("Línea 7: la zona 99 no existe.") separados en línea
 * y mensaje, para mostrarlos en una tabla. Los errores del archivo entero (columnas que faltan) no tienen línea.
 */
export function errorsByLine(errors: string[]): { line: number | null; message: string }[] {
  return errors
    .map((error) => {
      const match = /^L[ií]nea\s+(\d+):\s*(.+)$/i.exec(error.trim());
      return match ? { line: Number(match[1]), message: match[2]! } : { line: null, message: error.trim() };
    })
    .sort((a, b) => (a.line ?? 0) - (b.line ?? 0));
}
