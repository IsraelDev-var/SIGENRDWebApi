

using SIGENRD.Infrastructure.Identity.DTOs;

namespace SIGENRD.Infrastructure.Identity.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<string> ConfirmEmailAsync(string userId, string code);
    }
}
