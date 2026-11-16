using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Settings;
using SIGENRDWebApi.Constants;
using SIGENRDWebApi.Services;
using SIGENRD.Infrastructure.Identity.DTOs;
using SIGENRD.Infrastructure.Identity.Interfaces;
using SIGENRD.Infrastructure.Identity.Services;




namespace SIGENRDWebApi.Controllers.v1.Auth
{
    // AuthController.cs - Versión final limpia
    [ApiVersion("1.0")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public class AuthController(IAuthService authService, IMediator mediator) : BaseApiController
    {
        private readonly IAuthService _authService = authService;

        /// <summary>
        /// Registro público. Solo permite crear clientes e instaladoras (RF-06):
        /// los roles internos los asigna un administrador con register-internal.
        /// </summary>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request.UserType is not (UserType.Customer or UserType.InstallerCompany))
                throw new ApiException("El registro público solo admite los roles Customer e InstallerCompany.");

            // Pertenecer a una EDE solo lo decide un administrador.
            request.DistributorId = null;

            // Misma respuesta exista o no la cuenta (SEC-08); solo fallan los datos inválidos.
            var result = await _authService.RegisterAsync(request, revealExistingAccount: false);
            if (!result.Success)
                throw new ApiException(result.Message!); // ← 400

            return Ok(result);
        }

        /// <summary>Alta de usuarios internos (Engineer, Distributor, Admin). Solo un administrador.</summary>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpPost("register-internal")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RegisterInternal([FromBody] RegisterRequest request)
        {
            if (request.UserType is UserType.Customer or UserType.InstallerCompany)
                throw new ApiException("Los clientes e instaladoras se registran por el registro público.");

            // El ingeniero y la distribuidora trabajan para una EDE y solo ven lo de ella; el administrador, para todas.
            if (UserAdministration.StaffTypes.Contains(request.UserType))
            {
                if (request.DistributorId is not int distributorId)
                    throw new ApiException("Elige la distribuidora a la que pertenece.");
                await StaffDistributors.EnsureAssignableAsync(mediator, distributorId, HttpContext.RequestAborted);
            }
            else
            {
                request.DistributorId = null;
            }

            // La contraseña la eligió el administrador: la persona la cambia en su primer ingreso.
            var result = await _authService.RegisterAsync(request, revealExistingAccount: true, mustChangePassword: true);
            if (!result.Success)
                throw new ApiException(result.Message!);

            return Ok(result);
        }

        /// <summary>Cambia la contraseña con la sesión iniciada. Devuelve un token nuevo. Obligatorio tras el alta interna.</summary>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ForbiddenException();
            var result = await _authService.ChangePasswordAsync(userId, request);
            if (!result.Success)
                throw new ApiException(result.Message!);
            return Ok(result);
        }

        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (result.IsLockedOut)
                throw new TooManyRequestsException(result.Message!); // ← 429

            if (result.IsDeactivated)
                throw new ForbiddenException(result.Message!); // ← 403: la contraseña era correcta

            if (!result.Success)
                throw new ApiException("Credenciales inválidas"); // ← el middleware lo convierte en 400

            return Ok(result);
        }

        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
                throw new ApiException("Parámetros inválidos");

            var result = await _authService.ConfirmEmailAsync(userId, code);
            if (!result.Success)
                throw new ApiException(result.Message!); // ← 400: enlace inválido o ya usado

            return Ok(result);
        }

        /// <summary>Pide el enlace para restablecer la contraseña. Responde lo mismo exista o no la cuenta.</summary>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok(new AuthResponse { Success = true, Message = AuthService.ForgotPasswordMessage });
        }

        /// <summary>Fija la nueva contraseña con el código recibido por correo y levanta un bloqueo activo.</summary>
        [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.Success)
                throw new ApiException(result.Message!); // ← 400

            return Ok(result);
        }
    }
}