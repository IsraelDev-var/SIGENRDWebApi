using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Identity.Services;
using SIGENRDWebApi.Constants;

namespace SIGENRDWebApi.Controllers.v1.Users
{
    /// <summary>Usuarios internos (B-5, matriz 3.2): solo administradores. El alta sigue en POST /Auth/register-internal.</summary>
    [ApiVersion("1.0")]
    [Authorize(Roles = Roles.Admin)]
    public class UsersController(IUserAdministration users) : BaseApiController
    {
        /// <summary>Ingenieros, distribuidoras y administradores; activos primero. Filtra por nombre o correo y por rol.</summary>
        [ProducesResponseType<Response<PagedDto<InternalUserDto>>>(StatusCodes.Status200OK)]
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? text, [FromQuery] UserType? role,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
            => Ok(new Response<PagedDto<InternalUserDto>>(await users.ListInternalAsync(text, role, page, pageSize, cancellationToken)));

        /// <summary>No puede iniciar sesión hasta que se reactive. No se puede desactivar la propia cuenta ni al último administrador.</summary>
        [ProducesResponseType<Response<InternalUserDto>>(StatusCodes.Status200OK)]
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
            => Ok(new Response<InternalUserDto>(await users.SetActiveAsync(id, active: false, cancellationToken), "Cuenta desactivada."));

        [ProducesResponseType<Response<InternalUserDto>>(StatusCodes.Status200OK)]
        [HttpPost("{id}/reactivate")]
        public async Task<IActionResult> Reactivate(string id, CancellationToken cancellationToken)
            => Ok(new Response<InternalUserDto>(await users.SetActiveAsync(id, active: true, cancellationToken), "Cuenta reactivada."));
    }
}
