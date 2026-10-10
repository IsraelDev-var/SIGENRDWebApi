using SIGENRD.Core.Domain.Interfaces;
using System.Security.Claims;

namespace SIGENRDWebApi.Services
{
    /// <summary>
    /// Obtiene el usuario autenticado de la petición HTTP actual.
    /// </summary>
    public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
    {
        public string? UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
