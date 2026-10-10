namespace SIGENRD.Core.Application.Exceptions
{
    /// <summary>
    /// El usuario está autenticado pero no puede actuar sobre ese recurso (RN-22). Responde 403.
    /// </summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("No tienes permiso para actuar sobre este recurso.") { }

        public ForbiddenException(string message) : base(message) { }
    }
}
