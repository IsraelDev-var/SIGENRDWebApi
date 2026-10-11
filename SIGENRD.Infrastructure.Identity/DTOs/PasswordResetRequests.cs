using System.ComponentModel.DataAnnotations;

namespace SIGENRD.Infrastructure.Identity.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>Código del enlace enviado por correo, tal como llega en la URL.</summary>
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
