using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SIGENRD.Core.Application.DTOs.Email;
using SIGENRD.Core.Application.Interfaces.Services;
using SIGENRD.Infrastructure.Identity.DTOs;
using SIGENRD.Infrastructure.Identity.Entities;
using SIGENRD.Infrastructure.Identity.Interfaces;
using SIGENRD.Infrastructure.Identity.JWT;
using System.Text;

namespace SIGENRD.Infrastructure.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            JwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _emailService = emailService;
        }

        // -------------------------
        // REGISTRO DE USUARIO
        // -------------------------
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "El correo ya está registrado."
                };
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
            // (En producción, "https://localhost:7135" debe venir de appsettings como 'ClientUrl')
            var origin = "https://localhost:7135";
            var route = "api/v1/Auth/confirm-email";
            var verificationUri = $"{origin}/{route}?userId={user.Id}&code={verificationToken}";

            // 4. Enviar el correo (Fire and forget, o await si es crítico)
            await _emailService.SendAsync(new EmailRequest
            {
                To = user.Email,
                Subject = "Bienvenido a SIGENRD - Confirma tu correo",
                Body = $@"
            <h3>¡Registro Exitoso!</h3>
            <p>Hola {user.FullName}, gracias por registrarte.</p>
            <p>Por favor confirma tu cuenta haciendo clic en el siguiente enlace:</p>
            <a href='{verificationUri}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none;'>Confirmar Correo</a>
        "
            });

            // =========================================================================
            // 📧 FIN LÓGICA DE CORREO
            // =========================================================================

            // Nota: Opcionalmente, podrías NO devolver el token JWT aquí para obligar 
            // al usuario a confirmar el correo antes de loguearse.
            var jwtToken = _jwtTokenGenerator.GenerateToken(user, new List<string> { role });

            return new AuthResponse
            {
                Success = true,
                Message = "Usuario registrado. Por favor revise su correo para confirmar la cuenta.",
                Token = jwtToken,
                Email = user.Email,
                FullName = user.FullName,
                Role = role,
                UserId = user.Id
            };
        }

        // -------------------------
        // LOGIN
        // -------------------------
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Usuario o contraseña inválidos."
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "Usuario o contraseña inválidos."
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenGenerator.GenerateToken(user, roles);

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

            return result.Succeeded ? "Correo confirmado exitosamente" : "Error confirmando correo";
        }


    }
}
