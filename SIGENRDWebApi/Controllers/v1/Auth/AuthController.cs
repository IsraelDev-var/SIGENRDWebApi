using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Infrastructure.Identity.Interfaces;
using SIGENRD.Infrastructure.Identity.DTOs;
using SIGENRDWebApi.Controllers;


namespace SIGENRDWebApi.Controllers.v1.Auth
{
    [ApiVersion("1.0")]
    public class AuthController(IAuthService authService) : BaseApiController  // Ojo: BasecApiController parece un error de dedo (BaseApiController)
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Ahora 'request' es de tipo SIGENRD.Infrastructure.Identity.DTOs.RegisterRequest
                if (request == null) return BadRequest("Invalid registration request.");

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Recomendación: No devuelvas el objeto 'ex' completo al cliente por seguridad, solo el mensaje.
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // LoginRequest también tenía el mismo conflicto, al quitar el using de Microsoft se arregla solo.
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }
    }
}