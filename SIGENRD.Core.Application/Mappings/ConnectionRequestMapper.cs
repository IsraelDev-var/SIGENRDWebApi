using SIGENRD.Core.Application.DTOs.ConnectionRequest;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Core.Application.Mappings
{
    /// <summary>
    /// Conversión manual: las coordenadas salen de un Point (X = longitud, Y = latitud).
    /// </summary>
    public static class ConnectionRequestMapper
    {
        public static ConnectionRequestResponseDto ToResponse(ConnectionRequest r) => new()
        {
            Id = r.Id,
            RequestCode = r.RequestCode,
            Status = r.Status.ToString(),
            RequestedAt = r.RequestedAt,
            ProjectDescription = r.ProjectDescription,
            ProjectAddress = r.ProjectAddress,
            Latitude = r.Coordinates?.Y ?? 0,
            Longitude = r.Coordinates?.X ?? 0,
            UsageType = r.UsageType.ToString(),
            TariffType = r.TariffType.ToString(),
            InterconnectionType = r.InterconnectionType.ToString(),
            RequestType = r.RequestType.ToString(),
            RequestedCapacityKva = r.RequestedCapacityKva,
            PropertyRelation = r.PropertyRelation.ToString(),
            OwnerFullName = r.OwnerFullName,
            OwnerNationalId = r.OwnerNationalId,
            OwnerPhone = r.OwnerPhone,
            EngineerFullName = r.EngineerFullName,
            EngineerCodiaNumber = r.EngineerCodiaNumber,
            EngineerCompany = r.EngineerCompany,
            EngineerPhone = r.EngineerPhone,
            EngineerEmail = r.EngineerEmail,
            ServiceContractNumber = r.ServiceContractNumber,
            MeterNumber = r.MeterNumber,
            CustomerId = r.CustomerId,
            InstallerCompanyId = r.InstallerCompanyId,
            DistributorId = r.DistributorId,
            TransformerId = r.TransformerId,
            TransformerCode = r.Transformer?.Code,
            HolderName = r.Customer?.FullName,
            InstallerCompanyName = r.InstallerCompany?.TradeName,
            NetMetering = r.NetMeteringRequest is { } n
                ? new NetMeteringDetailsDto
                {
                    GenerationSystemType = n.GenerationSystemType.ToString(),
                    InstalledCapacityKw = n.InstalledCapacityKw,
                    Comments = n.Comments,
                    Generators = (n.Generators ?? []).Select(g => new GeneratorDto
                    {
                        GenerationSystemType = g.GenerationSystemType.ToString(),
                        GeneratorKind = g.GeneratorKind.ToString(),
                        Manufacturer = g.Manufacturer,
                        InverterModel = g.InverterModel,
                        SerialNumber = g.SerialNumber,
                        Phases = g.Phases,
                        RatedPowerKva = g.RatedPowerKva,
                        ConnectionVoltage = g.ConnectionVoltage,
                        NominalCurrent = g.NominalCurrent,
                        PowerFactorMin = g.PowerFactorMin,
                        PowerFactorMax = g.PowerFactorMax,
                        SwitchingType = g.SwitchingType,
                        HarmonicDistortionPercent = g.HarmonicDistortionPercent,
                        CertificationStandard = g.CertificationStandard,
                        CertifyingUnit = g.CertifyingUnit
                    }).ToList()
                }
                : null
        };
    }
}
