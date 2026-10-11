using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Exceptions
{
    /// <summary>Los documentos de la solicitud no se pueden cambiar en su estado actual (RF-16, RN-19). Responde 409.</summary>
    public class DocumentsLockedException(RequestStatus status)
        : Exception("Los documentos no se pueden modificar con la solicitud en estado " + status + ".")
    {
        public RequestStatus Status { get; } = status;
    }
}
