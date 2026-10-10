using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Settings;
using SIGENRDWebApi.Constants;
using SIGENRD.Infrastructure.Identity.DTOs;
using SIGENRD.Infrastructure.Identity.Interfaces;




namespace SIGENRDWebApi.Controllers.v1.Auth
{
    // AuthController.cs - Versión final limpia
    [ApiVersion("1.0")]
    public class AuthController(IAuthService authService) : BaseApiController
    {
        private readonly IAuthService _authService = authService;

        /// <summary>
        /// Registro público. Solo permite crear clientes e instaladoras (RF-06):
        /// los roles internos los asigna un administrador con register-internal.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request.UserType is not (UserType.Customer or UserType.InstallerCompany))
                throw new ApiException("El registro público solo admite los roles Customer e InstallerCompany.");

            var result = await _authService.RegisterAsync(request);

            return Ok(result); // Si falla, lanza excepción → la atrapa el middleware
        }

        /// <summary>Alta de usuarios internos (Engineer, Distributor, Admin). Solo un administrador.</summary>
        [HttpPost("register-internal")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RegisterInternal([FromBody] RegisterRequest request)
        {
            if (request.UserType is UserType.Customer or UserType.InstallerCompany)
                throw new ApiException("Los clientes e instaladoras se registran por el registro público.");

            return Ok(await _authService.RegisterAsync(request));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                throw new ApiException("Credenciales inválidas"); // ← el middleware lo convierte en 400

            return Ok(result);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
                throw new ApiException("Parámetros inválidos");

            var result = await _authService.ConfirmEmailAsync(userId, code);
            return Ok(result);
        }
    }
}