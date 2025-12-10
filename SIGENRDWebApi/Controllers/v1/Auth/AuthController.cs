using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Core.Domain.Settings;
using SIGENRD.Infrastructure.Identity.DTOs;
using SIGENRD.Infrastructure.Identity.Interfaces;




namespace SIGENRDWebApi.Controllers.v1.Auth
{
    // AuthController.cs - Versión final limpia
    [ApiVersion("1.0")]
    public class AuthController(IAuthService authService) : BaseApiController
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var mailSettings = HttpContext.RequestServices.GetRequiredService<IOptions<MailSettings>>().Value;
            Console.WriteLine($"EmailFrom: '{mailSettings.EmailFrom}'");
            Console.WriteLine($"DisplayName: '{mailSettings.DisplayName}'");

            var result = await _authService.RegisterAsync(request);

            return Ok(result); // Si falla, lanza excepción → la atrapa el middleware
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