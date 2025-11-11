

using SIGENRD.Core.Domain.Base;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un cliente final (persona o empresa) que solicita conexión eléctrica.
    /// </summary>
    public class Customer : AuditableEntity
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        // Cedula o RNC
        public string NationalIdOrRnc { get; set; } = string.Empty;

        // Correo electrónico
        public string? Email { get; set; }

        // Teléfono de contacto
        public string? Phone { get; set; }

        // Dirección física
        public string? Address { get; set; }

        
       

        // Relaciones
        public ICollection<ConnectionRequest>? ConnectionRequests { get; set; }
    }

}