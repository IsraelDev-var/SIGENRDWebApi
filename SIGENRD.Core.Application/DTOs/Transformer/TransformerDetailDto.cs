namespace SIGENRD.Core.Application.DTOs.Transformer
{
    /// <summary>Detalle técnico al seleccionar un transformador (RF-05).</summary>
    public class TransformerDetailDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;

        public int DistributorId { get; set; }
        public string? DistributorName { get; set; }

        public int ServiceZoneId { get; set; }
        public string? ZoneName { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public decimal TotalCapacityKva { get; set; }
        public decimal AvailableCapacityKva { get; set; }
        public decimal ReservedCapacityKva { get; set; }
        public decimal AvailabilityPct { get; set; }

        public string Status { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool AcceptsNewRequests { get; set; }

        // Los datos son referenciales: se muestra cuándo se actualizaron por última vez (RD-02)
        public DateTime LastUpdated { get; set; }
    }
}
