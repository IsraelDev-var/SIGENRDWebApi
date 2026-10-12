

using SIGENRD.Core.Domain.Base;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Representa un cliente final (persona o empresa) que solicita conexión eléctrica.
    /// </summary>
    public class Customer : AuditableEntity
    {
        public int Id { get; set; }
        /// <summary>Cuenta del cliente. Nula si lo registró una instaladora al solicitar a su nombre (B-11).</summary>
        public string? ApplicationUserId { get; set; }
        public string FullName { get; set; } = string.Empty;

        // Cedula o RNC
        public string NationalIdOrRnc { get; set; } = string.Empty;

        // Persona jurídica: cambia los documentos que se exigen (RN-09)
        public bool IsLegalEntity { get; set; }

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