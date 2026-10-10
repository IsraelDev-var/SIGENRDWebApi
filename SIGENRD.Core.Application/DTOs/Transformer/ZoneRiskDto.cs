namespace SIGENRD.Core.Application.DTOs.Transformer
{
    /// <summary>Zona con transformadores críticos o saturados (RF-29).</summary>
    public class ZoneRiskDto
    {
        public int ServiceZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public int TotalTransformers { get; set; }
        public int Critical { get; set; }
        public int Saturated { get; set; }
    }
}
