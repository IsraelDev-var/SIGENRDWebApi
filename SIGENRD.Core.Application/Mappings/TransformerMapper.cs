using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Mappings
{
    /// <summary>
    /// Conversión manual de transformadores: las coordenadas salen de un Point (X = longitud, Y = latitud),
    /// algo que AutoMapper no resuelve de forma evidente.
    /// </summary>
    public static class TransformerMapper
    {
        public static string ColorOf(TransformerStatus status) => status switch
        {
            TransformerStatus.Available => "green",
            TransformerStatus.Conditioned => "yellow",
            TransformerStatus.Critical => "red",
            TransformerStatus.Saturated => "red",
            _ => "gray"
        };

        public static TransformerMapPointDto ToMapPoint(Transformer t) => new()
        {
            Id = t.Id,
            Code = t.Code,
            Latitude = t.Location.Y,
            Longitude = t.Location.X,
            AvailabilityPct = t.AvailabilityPct,
            Status = t.Status.ToString(),
            Color = ColorOf(t.Status)
        };

        public static TransformerDetailDto ToDetail(Transformer t) => new()
        {
            Id = t.Id,
            Code = t.Code,
            DistributorId = t.DistributorId,
            DistributorName = t.Distributor?.Name,
            ServiceZoneId = t.ServiceZoneId,
            ZoneName = t.ServiceZone?.ZoneName,
            Latitude = t.Location.Y,
            Longitude = t.Location.X,
            TotalCapacityKva = t.TotalCapacityKva,
            AvailableCapacityKva = t.AvailableCapacityKva,
            ReservedCapacityKva = t.ReservedCapacityKva,
            AvailabilityPct = t.AvailabilityPct,
            Status = t.Status.ToString(),
            Color = ColorOf(t.Status),
            AcceptsNewRequests = t.AcceptsNewRequests,
            LastUpdated = t.LastUpdated
        };
    }
}
