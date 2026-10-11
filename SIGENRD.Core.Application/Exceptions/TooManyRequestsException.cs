namespace SIGENRD.Core.Application.Exceptions
{
    /// <summary>
    /// Demasiados intentos en poco tiempo (por ejemplo, cuenta bloqueada tras fallos de inicio de sesión). Responde 429.
    /// </summary>
    public class TooManyRequestsException(string message) : Exception(message)
    {
    }
}
