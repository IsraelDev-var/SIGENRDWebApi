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
    }
}
