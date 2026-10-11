using SIGENRD.Infrastructure.Identity.DTOs;

namespace SIGENRD.Infrastructure.Identity.Interfaces
{
    public interface IAuthService
    {
        /// <param name="revealExistingAccount">
        /// false en el registro público: misma respuesta exista o no la cuenta. true en el alta interna (administrador).
        /// </param>
        Task<AuthResponse> RegisterAsync(RegisterRequest request, bool revealExistingAccount);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<string> ConfirmEmailAsync(string userId, string code);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
