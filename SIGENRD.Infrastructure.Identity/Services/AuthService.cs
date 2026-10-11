using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using SIGENRD.Core.Application.Diagnostics;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Core.Domain.Interfaces;
using SIGENRD.Infrastructure.Identity.DTOs;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.Interfaces;
using SIGENRD.Infrastructure.Identity.JWT;
using System.Text;
using System.Text.Encodings.Web;

namespace SIGENRD.Infrastructure.Identity.Services
{
    public class AuthService : IAuthService
    {
        /// <summary>Respuesta del registro público, igual exista o no la cuenta (no revela qué correos están registrados).</summary>
        public const string PublicRegisterMessage = "Registro recibido. Revise su correo para continuar.";

        public const string ForgotPasswordMessage =
            "Si el correo pertenece a una cuenta confirmada, recibirá un enlace para restablecer la contraseña.";

        public const string InvalidResetCodeMessage = "El enlace para restablecer la contraseña no es válido o ya venció.";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ISecurityAuditLog _securityAudit;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            JwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService,
            IConfiguration configuration,
            ISecurityAuditLog securityAudit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
            _configuration = configuration;
            _securityAudit = securityAudit;
        }

        private string PublicBaseUrl => (_configuration["App:PublicBaseUrl"] ?? "https://localhost:7135").TrimEnd('/');

        // -------------------------
        // REGISTRO DE USUARIO
        // -------------------------
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, bool revealExistingAccount)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // El alta interna la hace un administrador: a él sí se le dice.
                if (revealExistingAccount)
                    return new AuthResponse { Success = false, Message = "El correo ya está registrado." };

                // Registro público: mismas validaciones y misma respuesta que una cuenta nueva;
                // el aviso de que ya existe la cuenta solo le llega al dueño del correo.
                var passwordErrors = await ValidatePasswordAsync(
                    new ApplicationUser { Email = request.Email, UserName = request.Email }, request.Password);
                if (passwordErrors != null)
                    return new AuthResponse { Success = false, Message = passwordErrors };

                await SendAccountAlreadyExistsEmailAsync(existingUser);
                return PublicRegisterResponse(request);
            }

            var user = new ApplicationUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.Email,
                UserType = request.UserType,
                DistributorType = request.DistributorType,
                InstallerCompanyId = request.InstallerCompanyId,
                // Importante: El usuario nace sin confirmar
                IsEmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join("; ", result.Errors.Select(e => e.Description))
                };
            }

            // Asignar rol automático
            string role = request.UserType.ToString();
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new ApplicationRole { Name = role });

            await _userManager.AddToRoleAsync(user, role);

            // =========================================================================
            // 📧 INICIO LÓGICA DE CORREO
            // =========================================================================

            // 1. Generar token de identidad
            var verificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // 2. Codificar token para URL (evita errores con caracteres especiales)
            verificationToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(verificationToken));

            // 3. Construir URL de confirmación
            // La URL pública viene de la configuración (App:PublicBaseUrl).
            var route = "api/v1/Auth/confirm-email";
            var verificationUri = $"{PublicBaseUrl}/{route}?userId={user.Id}&code={verificationToken}";

            // 4. Enviar el correo
            await _emailService.SendAsync(new EmailRequest
            {
                To = user.Email,
                Subject = "Bienvenido a SIGENRD - Confirma tu correo",
                Body = $@"
            <h3>¡Registro Exitoso!</h3>
            <p>Hola {HtmlEncoder.Default.Encode(user.FullName)}, gracias por registrarte.</p>
            <p>Por favor confirma tu cuenta haciendo clic en el siguiente enlace:</p>
            <a href='{verificationUri}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none;'>Confirmar Correo</a>
        "
            });

            // =========================================================================
            // 📧 FIN LÓGICA DE CORREO
            // =========================================================================

            // No se entrega token en el registro: la cuenta debe confirmarse por correo antes de iniciar sesión.

            if (!revealExistingAccount)
                return PublicRegisterResponse(request);

            return new AuthResponse
            {
                Success = true,
                Message = "Usuario registrado. Por favor revise su correo para confirmar la cuenta.",
                Email = user.Email,
                FullName = user.FullName,
                Role = role,
                UserId = user.Id
            };
        }

        private static AuthResponse PublicRegisterResponse(RegisterRequest request) => new()
        {
            Success = true,
            Message = PublicRegisterMessage,
            Email = request.Email,
            Role = request.UserType.ToString()
        };

        /// <summary>Errores de la política de contraseñas, o null si la contraseña es aceptable.</summary>
        private async Task<string?> ValidatePasswordAsync(ApplicationUser user, string password)
        {
            var errors = new List<string>();
            foreach (var validator in _userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, user, password);
                errors.AddRange(result.Errors.Select(e => e.Description));
            }

            return errors.Count == 0 ? null : string.Join("; ", errors);
        }

        private Task SendAccountAlreadyExistsEmailAsync(ApplicationUser user) => _emailService.SendAsync(new EmailRequest
        {
            To = user.Email!,
            Subject = "SIGENRD - Ya tienes una cuenta",
            Body = $@"
            <p>Hola {HtmlEncoder.Default.Encode(user.FullName)}:</p>
            <p>Alguien intentó registrarse con este correo, pero ya tienes una cuenta.</p>
            <p>Si olvidaste tu contraseña, puedes restablecerla desde la opción ""Olvidé mi contraseña"".
            Si no fuiste tú, puedes ignorar este mensaje.</p>
        "
        });

        // -------------------------
        // LOGIN
        // -------------------------
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                await _securityAudit.RecordAsync(SecurityEvents.LoginFailed, null, request.Email);
                EnergyMapMetrics.LoginFailed();
                return new AuthResponse
                {
                    Success = false,
                    Message = "Usuario o contraseña inválidos."
                };
            }

            // lockoutOnFailure: cada fallo cuenta para el bloqueo; un acierto reinicia el contador.
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                await _securityAudit.RecordAsync(SecurityEvents.LoginLockedOut, user.Id, request.Email);
                EnergyMapMetrics.LoginLockedOut();
                return new AuthResponse
                {
                    Success = false,
                    IsLockedOut = true,
                    Message = "Demasiados intentos fallidos. Intente de nuevo más tarde."
                };
            }

            if (!result.Succeeded)
            {
                await _securityAudit.RecordAsync(SecurityEvents.LoginFailed, user.Id, request.Email);
                EnergyMapMetrics.LoginFailed();
                return new AuthResponse
                {
                    Success = false,
                    Message = "Usuario o contraseña inválidos."
                };
            }

            // Las credenciales ya son válidas: se puede indicar el motivo sin revelar nada nuevo.
            if (!user.EmailConfirmed)
            {
                await _securityAudit.RecordAsync(SecurityEvents.LoginFailed, user.Id, request.Email);
                EnergyMapMetrics.LoginFailed();
                return new AuthResponse
                {
                    Success = false,
                    Message = "Debes confirmar tu correo antes de iniciar sesión."
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenGenerator.GenerateToken(user, roles);
            await _securityAudit.RecordAsync(SecurityEvents.LoginSucceeded, user.Id, request.Email);
            EnergyMapMetrics.LoginSucceeded();

            return new AuthResponse
            {
                Success = true,
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                Role = roles.FirstOrDefault(),
                UserId = user.Id
            };
        }

        // ========================== Confirmacion de Email


        public async Task<string> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return "Usuario no encontrado";

            // Decodificar el token
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);

            if (!result.Succeeded)
                return "Error confirmando correo";

            // Se mantiene sincronizada la bandera propia del usuario con la de Identity.
            user.IsEmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            return "Correo confirmado exitosamente";
        }

        // ========================== Recuperación de contraseña

        /// <summary>
        /// Envía el enlace de restablecimiento si la cuenta existe y está confirmada.
        /// El llamador responde siempre lo mismo (<see cref="ForgotPasswordMessage"/>).
        /// </summary>
        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            await _securityAudit.RecordAsync(SecurityEvents.PasswordResetRequested, user?.Id, request.Email);

            if (user == null || !user.EmailConfirmed) return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Página del frontend que pide la nueva contraseña y llama a reset-password.
            var resetUrl = (_configuration["App:PasswordResetUrl"] ?? PublicBaseUrl + "/reset-password").TrimEnd('/');
            var link = $"{resetUrl}?email={Uri.EscapeDataString(user.Email!)}&code={code}";

            await _emailService.SendAsync(new EmailRequest
            {
                To = user.Email!,
                Subject = "SIGENRD - Restablecer contraseña",
                Body = $@"
            <p>Hola {HtmlEncoder.Default.Encode(user.FullName)}:</p>
            <p>Recibimos una solicitud para restablecer tu contraseña. El enlace vence en 24 horas y sirve una sola vez.</p>
            <a href='{link}'>Restablecer contraseña</a>
            <p>Si no lo pediste tú, ignora este mensaje: tu contraseña no cambia.</p>
        "
            });
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var invalid = new AuthResponse { Success = false, Message = InvalidResetCodeMessage };

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return invalid;

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
            }
            catch (FormatException)
            {
                return invalid;
            }

            // Identity comprueba el código antes que la contraseña: sin código válido no se revela nada más.
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                    return invalid;

                return new AuthResponse
                {
                    Success = false,
                    Message = string.Join("; ", result.Errors.Select(e => e.Description))
                };
            }

            // Quien restablece desde su correo demuestra ser el dueño: se levanta el bloqueo.
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _securityAudit.RecordAsync(SecurityEvents.PasswordResetCompleted, user.Id, request.Email);

            return new AuthResponse { Success = true, Message = "Contraseña actualizada. Ya puede iniciar sesión." };
        }
    }
}
