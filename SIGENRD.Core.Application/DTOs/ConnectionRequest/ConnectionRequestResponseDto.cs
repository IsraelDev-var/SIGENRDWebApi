

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

        public string RequestType { get; set; } = string.Empty;
        public decimal RequestedCapacityKva { get; set; }

        // Formulario de solicitud
        public string PropertyRelation { get; set; } = string.Empty;
        public string? OwnerFullName { get; set; }
        public string? EngineerFullName { get; set; }
        public string? EngineerCodiaNumber { get; set; }
        public string? ServiceContractNumber { get; set; }
        public string? MeterNumber { get; set; }

        // IDs Relacionados
        public int CustomerId { get; set; }
        public int? InstallerCompanyId { get; set; }
        public int DistributorId { get; set; }
        public int? TransformerId { get; set; }
        // Para el expediente y las listas
        public string? TransformerCode { get; set; }
        /// <summary>Titular del servicio (el cliente).</summary>
        public string? HolderName { get; set; }
        public string? InstallerCompanyName { get; set; }
    }
}
