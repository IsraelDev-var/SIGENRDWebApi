using SIGENRD.Core.Domain.Base;

namespace SIGENRD.Core.Domain.Entities
{
    /// <summary>
    /// Datos de perfil del usuario que no forman parte del esquema base de Identity.
    /// </summary>
    public class UserProfile : AuditableEntity
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
