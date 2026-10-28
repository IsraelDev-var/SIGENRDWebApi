using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Interfaces.Services
{
    /// <summary>Permiso para subir un documento, con su vencimiento.</summary>
    public record DocumentUploadTicket(string Ticket, DateTime ExpiresAt);

    /// <summary>
    /// Permisos de subida directa: el navegador sube el archivo a la API sin pasar por el servidor del frontend, que en
    /// algunas plataformas no acepta cuerpos de 10 MB (RN-18). El permiso vale para una solicitud, un tipo de documento
    /// y un solo uso, y vence en minutos; no sirve como token de sesión en ninguna otra ruta.
    /// </summary>
    public interface IDocumentUploadTickets
    {
        DocumentUploadTicket Issue(string userId, string fullName, string role, int requestId, DocumentType documentType);
    }

    /// <summary>Nombres compartidos entre quien emite el permiso y el endpoint que lo recibe.</summary>
    public static class DocumentUploadTicketDefaults
    {
        /// <summary>Esquema de autenticación que solo acepta permisos de subida.</summary>
        public const string Scheme = "DocumentUpload";

        /// <summary>Se agrega a la audiencia de los tokens de sesión: un permiso no se acepta como sesión ni al revés.</summary>
        public const string AudienceSuffix = ".document-upload";

        public const string RequestIdClaim = "req";
        public const string DocumentTypeClaim = "doc";

        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    }
}
