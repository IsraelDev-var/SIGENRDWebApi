namespace SIGENRD.Core.Domain.Enums
{
    /// <summary>
    /// Estados del transformador (RN-02). Maintenance es administrativo y no se calcula.
    /// </summary>
    public enum TransformerStatus
    {
        Available = 1,
        Conditioned = 2,
        Critical = 3,
        Saturated = 4,
        Maintenance = 5
    }
}
