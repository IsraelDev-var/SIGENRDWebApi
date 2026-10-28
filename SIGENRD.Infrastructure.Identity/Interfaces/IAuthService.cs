using SIGENRD.Infrastructure.Identity.DTOs;

namespace SIGENRD.Infrastructure.Identity.Interfaces
{
    public interface IAuthService
    {
        /// <param name="revealExistingAccount">
        /// false en el registro público: misma respuesta exista o no la cuenta. true en el alta interna (administrador).
        /// </param>
        /// <param name="mustChangePassword">true en el alta interna: la contraseña la eligió el administrador.</param>
        Task<AuthResponse> RegisterAsync(RegisterRequest request, bool revealExistingAccount, bool mustChangePassword = false);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> ConfirmEmailAsync(string userId, string code);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>Cambia la contraseña del usuario autenticado y devuelve un token nuevo (sin la marca de temporal).</summary>
        Task<AuthResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    }
}
