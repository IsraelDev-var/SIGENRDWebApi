namespace SIGENRD.Core.Application.DTOs.Transformer
{
    /// <summary>
    /// Punto ligero para pintar el mapa (RF-01, RF-02). Solo lo necesario para dibujar y colorear.
    /// </summary>
    public class TransformerMapPointDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal AvailabilityPct { get; set; }
        public string Status { get; set; } = string.Empty;

        // green, yellow, red o gray (RN-02)
        public string Color { get; set; } = string.Empty;
    }
}
