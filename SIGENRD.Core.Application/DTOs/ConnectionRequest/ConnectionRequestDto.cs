

namespace SIGENRD.Core.Application.DTOs.ConnectionRequest
{
    public class ConnectionRequestDto
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;

        // Devolvemos el estatus como string para que sea legible en el JSON
        // (Aunque también podrías devolver el int si el front usa el Enum)
        public string Status { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; }

        // IDs Relacionados
        public int CustomerId { get; set; }
        public int InstallerCompanyId { get; set; }
        public int DistributorId { get; set; }
        public int? TransformerId { get; set; }

        // Datos Técnicos (Como strings descriptivos o ints según prefieras)
        public string UsageType { get; set; } = string.Empty;
        public string TariffType { get; set; } = string.Empty;
        public string InterconnectionType { get; set; } = string.Empty;

        public string? ProjectDescription { get; set; }
        public string? ProjectAddress { get; set; }

        // 🌍 Coordenadas "aplanadas" para fácil lectura
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
