

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
        public string? OwnerNationalId { get; set; }
        public string? OwnerPhone { get; set; }
        public string? EngineerFullName { get; set; }
        public string? EngineerCodiaNumber { get; set; }
        public string? EngineerCompany { get; set; }
        public string? EngineerPhone { get; set; }
        public string? EngineerEmail { get; set; }
        public string? ServiceContractNumber { get; set; }
        public string? MeterNumber { get; set; }
        /// <summary>Sistema de generación (solo medición neta y solo en el detalle; null en las listas).</summary>
        public NetMeteringDetailsDto? NetMetering { get; set; }

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

    public class NetMeteringDetailsDto
    {
        public string GenerationSystemType { get; set; } = string.Empty;
        public decimal InstalledCapacityKw { get; set; }
        public string? Comments { get; set; }
        public List<GeneratorDto> Generators { get; set; } = new();
    }

    public class GeneratorDto
    {
        public string GenerationSystemType { get; set; } = string.Empty;
        public string GeneratorKind { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string? InverterModel { get; set; }
        public string? SerialNumber { get; set; }
        public int Phases { get; set; }
        public decimal RatedPowerKva { get; set; }
        public decimal ConnectionVoltage { get; set; }
        public decimal NominalCurrent { get; set; }
        public decimal PowerFactorMin { get; set; }
        public decimal PowerFactorMax { get; set; }
        public string? SwitchingType { get; set; }
        public decimal HarmonicDistortionPercent { get; set; }
        public string? CertificationStandard { get; set; }
        public string? CertifyingUnit { get; set; }
    }
}
