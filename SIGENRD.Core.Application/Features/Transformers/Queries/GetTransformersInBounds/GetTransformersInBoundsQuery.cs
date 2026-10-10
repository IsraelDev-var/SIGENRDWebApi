using MediatR;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformersInBounds
{
    /// <summary>
    /// Transformadores dentro del área visible del mapa (RF-01 a RF-03).
    /// El cliente envía el rectángulo del viewport y los filtros activos.
    /// </summary>
    public class GetTransformersInBoundsQuery : IRequest<Response<TransformerMapResultDto>>
    {
        public double MinLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MaxLongitude { get; set; }

        public int? DistributorId { get; set; }
        public int? ServiceZoneId { get; set; }
        public TransformerStatus? Status { get; set; }
        public decimal? MinAvailabilityPct { get; set; }

        public int Limit { get; set; } = DefaultLimit;

        public const int DefaultLimit = 2000;
        public const int MaxLimit = 5000;
    }
}
