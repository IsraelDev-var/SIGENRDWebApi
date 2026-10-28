

using Microsoft.AspNetCore.Identity;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Infrastructure.Identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }

        public UserType UserType { get; set; }  // Customer / Engineer / Company / Distributor
        public DistributorType? DistributorType { get; set; }

        public int? InstallerCompanyId { get; set; }

        /// <summary>Cuenta desactivada por un administrador (B-5): no puede iniciar sesión. Null si está activa.</summary>
        public DateTime? DeactivatedAt { get; set; }

        /// <summary>La contraseña la puso un administrador: hay que cambiarla antes de usar el sistema.</summary>
        public bool MustChangePassword { get; set; }

    }
}
