using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.SearchTransformers
{
    /// <summary>
    /// Búsqueda por código o por cercanía a unas coordenadas (RF-04).
    /// Buscar por dirección requiere geocodificar primero: el cliente convierte la dirección
    /// en coordenadas y usa la búsqueda por cercanía.
    /// </summary>
    public class SearchTransformersQuery : IRequest<Response<List<TransformerMapPointDto>>>
    {
        // Búsqueda por código (coincidencia parcial, sin distinguir mayúsculas)
        public string? Code { get; set; }

        // Búsqueda por cercanía
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double RadiusMeters { get; set; } = DefaultRadiusMeters;

        public int? DistributorId { get; set; }
        public int? ServiceZoneId { get; set; }
        public TransformerStatus? Status { get; set; }
        public decimal? MinAvailabilityPct { get; set; }

        public int Limit { get; set; } = DefaultLimit;

        public const double DefaultRadiusMeters = 1000;
        public const double MaxRadiusMeters = 20000;
        public const int DefaultLimit = 50;
        public const int MaxLimit = 200;
    }
}
