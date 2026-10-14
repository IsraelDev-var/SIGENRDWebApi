namespace SIGENRD.Infrastructure.Identity.DTOs
{
    public class AuthResponse
    {
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Token { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }

        /// <summary>La cuenta está bloqueada temporalmente por intentos fallidos: no se devuelve en el JSON.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsLockedOut { get; set; }

        /// <summary>La contraseña es correcta pero un administrador desactivó la cuenta (B-5).</summary>
        public bool IsDeactivated { get; set; }
    }
}
