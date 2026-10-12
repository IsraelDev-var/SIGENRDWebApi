using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Features.ConnectionRequests.Commands.CreateConnectionRequest
{
    /// <summary>
    /// Datos del proyecto de una solicitud: los mismos al crear el borrador y al editarlo (D-4).
    /// </summary>
    public abstract class ConnectionRequestData
    {
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
        public PropertyRelation PropertyRelation { get; set; } = PropertyRelation.Owner;
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
        public bool DeclarationAccepted { get; set; }
        public NetMeteringDetailsInput? NetMetering { get; set; }
    }

    /// <summary>
    /// Crea una solicitud como borrador (D-4): no avisa a nadie ni reserva capacidad hasta que el solicitante la envía.
    /// El cliente la crea a su nombre (su perfil). La instaladora validada la crea a nombre de un cliente con
    /// <see cref="Holder"/> (B-11): se usa el cliente con esa cédula o RNC, o se registra uno sin cuenta.
    /// </summary>
    public class CreateConnectionRequestCommand : ConnectionRequestData, IRequest<Response<int>>
    {
        /// <summary>Opcional para el cliente (se toma de su perfil); si se envía, debe ser el suyo.</summary>
        public int? CustomerId { get; set; }
        /// <summary>Opcional para la instaladora (se toma de su empresa); si se envía, debe ser la suya.</summary>
        public int? InstallerCompanyId { get; set; }
        /// <summary>Titular del servicio cuando solicita una instaladora. Obligatorio para la instaladora.</summary>
        public HolderInput? Holder { get; set; }
    }

    /// <summary>Edita los datos del proyecto mientras la solicitud es un borrador.</summary>
    public class UpdateDraftRequestCommand : ConnectionRequestData, IRequest<Response<ConnectionRequestResponseDto>>
    {
        public int Id { get; set; }
    }

    /// <summary>Titular del servicio indicado por la instaladora (B-11).</summary>
    public class HolderInput
    {
        public string FullName { get; set; } = string.Empty;
        public string NationalIdOrRnc { get; set; } = string.Empty;
        public bool IsLegalEntity { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
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
        public int Phases { get; set; } = 1;
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
