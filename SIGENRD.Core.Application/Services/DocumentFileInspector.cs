using System.Text;
using SIGENRD.Core.Application.Exceptions;

namespace SIGENRD.Core.Application.Services
{
    /// <summary>
    /// Valida un archivo antes de guardarlo (RN-18, RF-36): PDF, JPG o PNG de hasta 10 MB.
    /// El tipo se decide por el contenido (firma del archivo), no por la extensión ni por lo que declare el navegador,
    /// y los PDF con contenido activo (JavaScript, acciones de lanzamiento o archivos incrustados) se rechazan.
    /// No sustituye a un antivirus.
    /// </summary>
    public static class DocumentFileInspector
    {
        public const long MaxSizeBytes = 10 * 1024 * 1024;

        public record Inspected(string ContentType, string Extension, string SafeFileName);

        private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };

        // Marcas de contenido activo en un PDF. Un documento técnico escaneado o exportado no las necesita.
        // /OpenAction y /AA no se rechazan solos: los PDF normales los usan para la vista inicial; lo peligroso que
        // pueden disparar (JavaScript, Launch) ya está en la lista. Los nombres dentro de flujos comprimidos no se ven.
        private static readonly string[] ActivePdfMarkers = { "/JavaScript", "/JS", "/Launch", "/EmbeddedFile", "/RichMedia", "/XFA" };

        public static Inspected Inspect(string fileName, byte[] content)
        {
            if (content.Length == 0)
                throw new ApiException("El archivo está vacío.");
            if (content.Length > MaxSizeBytes)
                throw new ApiException("El archivo supera el máximo de 10 MB.");

            var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

            var (contentType, allowedExtensions) =
                StartsWith(content, PdfSignature) ? ("application/pdf", new[] { ".pdf" }) :
                StartsWith(content, PngSignature) ? ("image/png", new[] { ".png" }) :
                StartsWith(content, JpegSignature) ? ("image/jpeg", new[] { ".jpg", ".jpeg" }) :
                throw new ApiException("Formato no permitido: solo PDF, JPG o PNG.");

            if (!allowedExtensions.Contains(extension))
                throw new ApiException("La extensión del archivo no coincide con su contenido (" + contentType + ").");

            if (contentType == "application/pdf" && HasActiveContent(content))
                throw new ApiException("El PDF contiene contenido activo (JavaScript, acciones o archivos incrustados) y no se acepta.");

            return new Inspected(contentType, allowedExtensions[0], SafeName(fileName!, extension));
        }

        private static bool StartsWith(byte[] content, byte[] signature) =>
            content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);

        private static bool HasActiveContent(byte[] content)
        {
            // Latin1 conserva cada byte como un carácter: sirve para buscar las palabras clave del PDF.
            var text = Encoding.Latin1.GetString(content);
            return ActivePdfMarkers.Any(marker =>
            {
                for (var i = text.IndexOf(marker, StringComparison.Ordinal); i >= 0; i = text.IndexOf(marker, i + 1, StringComparison.Ordinal))
                {
                    // Solo la palabra clave completa: /JS no debe coincidir con /JSomething.
                    var end = i + marker.Length;
                    if (end >= text.Length || !char.IsLetterOrDigit(text[end]))
                        return true;
                }
                return false;
            });
        }

        /// <summary>Nombre para mostrar y descargar: sin rutas, sin caracteres de control, acotado.</summary>
        private static string SafeName(string fileName, string extension)
        {
            var name = Path.GetFileNameWithoutExtension(Path.GetFileName(fileName.Replace('\\', '/')));
            var clean = new string(name.Where(c => !char.IsControl(c) && c is not ('"' or '<' or '>' or '|' or ':' or '*' or '?' or '/')).ToArray()).Trim();
            if (clean.Length == 0) clean = "documento";
            if (clean.Length > 200) clean = clean[..200];
            return clean + extension;
        }
    }
}
