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

        public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;

        public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
