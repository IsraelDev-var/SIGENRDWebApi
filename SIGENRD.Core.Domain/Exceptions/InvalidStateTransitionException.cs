using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Exceptions
{
    /// <summary>La solicitud no puede pasar del estado actual al pedido (RN-12). Responde 409.</summary>
    public class InvalidStateTransitionException(RequestStatus from, RequestStatus to)
        : Exception("La solicitud no puede pasar de " + from + " a " + to + ".")
    {
        public RequestStatus From { get; } = from;
        public RequestStatus To { get; } = to;
    }
}
