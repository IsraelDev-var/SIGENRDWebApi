namespace SIGENRD.Core.Domain.Exceptions
{
    /// <summary>
    /// Otro usuario modificó el mismo registro al mismo tiempo. El cliente debe volver a consultar y reintentar.
    /// Responde 409.
    /// </summary>
    public class ConcurrencyConflictException()
        : Exception("El registro fue modificado por otra persona. Consulta de nuevo e intenta otra vez.");
}
