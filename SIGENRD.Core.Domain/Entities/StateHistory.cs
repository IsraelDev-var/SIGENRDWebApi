using SIGENRD.Core.Domain.Base;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Registro del historial de cambios de estado de una solicitud.
    /// </summary>
    public class StateHistory : AuditableEntity
    {
        public int Id { get; set; }

        // Id de la solicitud relacionada
        public int ConnectionRequestId { get; set; }

        // Estado anterior
        public string? PreviousState { get; set; }

        // Nuevo estado
        public string? NewState { get; set; }

        

        // Comentario o motivo del cambio
        public string? Comment { get; set; }

        // Relación
        public ConnectionRequest? ConnectionRequest { get; set; }
    }
}
