

namespace SIGENRD.Core.Application.DTOs.ConnectionRequest
{
    public class ConnectionRequestResponseDto
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;

        // Estado y Fechas
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }

        // Datos del Proyecto
        public string? ProjectDescription { get; set; }
        public string? ProjectAddress { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Datos Técnicos (Strings para lectura fácil)
        public string UsageType { get; set; } = string.Empty;
        public string TariffType { get; set; } = string.Empty;
        public string InterconnectionType { get; set; } = string.Empty;

        // IDs Relacionados
        public int CustomerId { get; set; }
        public int InstallerCompanyId { get; set; }
        public int DistributorId { get; set; }
        public int? TransformerId { get; set; }
    }
}
