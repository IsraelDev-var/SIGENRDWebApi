using MediatR;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    // Este comando actúa como tu antiguo DTO de entrada, pero implementa IRequest
    public class CreateConnectionRequestCommand : IRequest<Response<int>>
    {
        public int CustomerId { get; set; }
        public int? InstallerCompanyId { get; set; }
        public int DistributorId { get; set; }
        public int TransformerId { get; set; }
        public RequestType RequestType { get; set; } = RequestType.Connection;
        public decimal RequestedCapacityKva { get; set; }
        public UsageType UsageType { get; set; }
        public TariffType TariffType { get; set; }
        public InterconnectionType InterconnectionType { get; set; }
        public string? ProjectDescription { get; set; }
        public string ProjectAddress { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // ---- Formulario de solicitud (Anexo A del reglamento de interconexión) ----

        /// <summary>Relación del solicitante con la propiedad (parte B).</summary>
        public PropertyRelation PropertyRelation { get; set; } = PropertyRelation.Owner;

        /// <summary>Datos del dueño; obligatorios cuando el solicitante no lo es (parte C).</summary>
        public string? OwnerFullName { get; set; }
        public string? OwnerNationalId { get; set; }
        public string? OwnerPhone { get; set; }

        /// <summary>Ingeniero Colegiado; obligatorio en medición neta y por encima de 10 kVA (parte D).</summary>
        public string? EngineerFullName { get; set; }
        public string? EngineerCodiaNumber { get; set; }
        public string? EngineerCompany { get; set; }
        public string? EngineerPhone { get; set; }
        public string? EngineerEmail { get; set; }

        /// <summary>Contrato y medidor del servicio existente; obligatorios en medición neta (parte E).</summary>
        public string? ServiceContractNumber { get; set; }
        public string? MeterNumber { get; set; }

        /// <summary>El solicitante certifica que la información es correcta (parte J).</summary>
        public bool DeclarationAccepted { get; set; }

        /// <summary>Datos del sistema de generación; obligatorios solo en medición neta (parte I).</summary>
        public NetMeteringDetailsInput? NetMetering { get; set; }
    }

    public class NetMeteringDetailsInput
    {
        public GenerationSystemType GenerationSystemType { get; set; }

        public decimal InstalledCapacityKw { get; set; }

        public string? Comments { get; set; }

        public List<GeneratorInput> Generators { get; set; } = new();
    }

    public class GeneratorInput
    {
        public GenerationSystemType GenerationSystemType { get; set; }
        public GeneratorKind GeneratorKind { get; set; } = GeneratorKind.InverterBased;

        public string Manufacturer { get; set; } = string.Empty;
        public string? InverterModel { get; set; }
        public string? SerialNumber { get; set; }

        /// <summary>1 monofásico, 3 trifásico.</summary>
        public int Phases { get; set; } = 1;

        public decimal RatedPowerKva { get; set; }
        public decimal ConnectionVoltage { get; set; }
        public decimal NominalCurrent { get; set; }
        public decimal PowerFactorMin { get; set; }
        public decimal PowerFactorMax { get; set; }
        public string? SwitchingType { get; set; }
        public decimal HarmonicDistortionPercent { get; set; }

        /// <summary>Estándar y entidad que certifican el equipo (parte G).</summary>
        public string? CertificationStandard { get; set; }
        public string? CertifyingUnit { get; set; }
    }
}
