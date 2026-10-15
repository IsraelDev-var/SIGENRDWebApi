using SIGENRD.Core.Application.Exceptions;
using SIGENRD.Infrastructure.Identity.JWT;

namespace SIGENRDWebApi.Middlewares
{
    /// <summary>
    /// Con una contraseña temporal (alta por un administrador) el token solo sirve para cambiarla: cualquier otra llamada
    /// autenticada recibe 403 con el motivo. Va después de UseAuthentication, que es quien lee el token.
    /// </summary>
    public class MustChangePasswordMiddleware(RequestDelegate next)
    {
        private static readonly PathString ChangePassword = "/api/v1/Auth/change-password";

        public Task Invoke(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true
                && context.User.HasClaim(JwtTokenGenerator.MustChangePasswordClaim, "true")
                && !context.Request.Path.StartsWithSegments(ChangePassword, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException("Debes cambiar la contraseña temporal antes de continuar.");
            }

            return next(context);
        }
    }
}
