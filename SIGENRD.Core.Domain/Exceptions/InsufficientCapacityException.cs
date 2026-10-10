namespace SIGENRD.Core.Domain.Exceptions
{
    /// <summary>La capacidad del transformador no cubre la potencia solicitada (RN-03, RN-15). Responde 409.</summary>
    public class InsufficientCapacityException(decimal requiredKva, decimal availableKva)
        : Exception("La capacidad disponible del transformador (" + availableKva.ToString("0.##") +
                    " kVA) no cubre la capacidad solicitada (" + requiredKva.ToString("0.##") + " kVA).")
    {
        public decimal RequiredKva { get; } = requiredKva;
        public decimal AvailableKva { get; } = availableKva;
    }
}
