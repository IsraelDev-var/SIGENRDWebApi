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
            EngineerFullName = r.EngineerFullName,
            EngineerCodiaNumber = r.EngineerCodiaNumber,
            ServiceContractNumber = r.ServiceContractNumber,
            MeterNumber = r.MeterNumber,
            CustomerId = r.CustomerId,
            InstallerCompanyId = r.InstallerCompanyId,
            DistributorId = r.DistributorId,
            TransformerId = r.TransformerId
        };
    }
}
