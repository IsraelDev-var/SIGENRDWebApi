using FluentValidation;
using SIGENRD.Core.Application.DTOs.Transformer;
using SIGENRD.Core.Application.Mappings;
using SIGENRD.Core.Application.Mediator;
using SIGENRD.Core.Application.Wrappers;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Core.Domain.Rules;

namespace SIGENRD.Core.Application.Features.Transformers.Queries.GetTransformerClusters
{
    /// <summary>
    /// Transformadores agrupados por zoom dentro del área visible (B-2). Sirve la vista del país sin enviar miles
    /// de puntos; al acercarse, el mapa pide los puntos con GET /Transformers.
    /// </summary>
    public class GetTransformerClustersQuery : IRequest<Response<TransformerClusterResultDto>>
    {
        public double MinLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MaxLongitude { get; set; }
        public double Zoom { get; set; }
        public int? DistributorId { get; set; }
        public int? ServiceZoneId { get; set; }
        public TransformerStatus? Status { get; set; }
        public decimal? MinAvailabilityPct { get; set; }
    }

    public class GetTransformerClustersValidator : AbstractValidator<GetTransformerClustersQuery>
    {
        public GetTransformerClustersValidator()
        {
            RuleFor(q => q.MinLatitude).InclusiveBetween(-90, 90).WithMessage("La latitud mínima debe estar entre -90 y 90.");
            RuleFor(q => q.MaxLatitude).InclusiveBetween(-90, 90).WithMessage("La latitud máxima debe estar entre -90 y 90.");
            RuleFor(q => q.MinLongitude).InclusiveBetween(-180, 180).WithMessage("La longitud mínima debe estar entre -180 y 180.");
            RuleFor(q => q.MaxLongitude).InclusiveBetween(-180, 180).WithMessage("La longitud máxima debe estar entre -180 y 180.");
            RuleFor(q => q)
                .Must(q => q.MinLatitude < q.MaxLatitude && q.MinLongitude < q.MaxLongitude)
                .WithMessage("El área solicitada no es válida: los mínimos deben ser menores que los máximos.");
            RuleFor(q => q.Zoom)
                .InclusiveBetween(TransformerGrid.MinZoom, TransformerGrid.MaxZoom)
                .WithMessage("El zoom debe estar entre " + TransformerGrid.MinZoom + " y " + TransformerGrid.MaxZoom + ".");
            RuleFor(q => q.MinAvailabilityPct)
                .InclusiveBetween(0m, 100m).When(q => q.MinAvailabilityPct.HasValue)
                .WithMessage("La disponibilidad mínima debe estar entre 0 y 100.");
            RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue).WithMessage("Estado de transformador inválido.");
        }
    }

    public class GetTransformerClustersHandler(ITransformerRepository repository)
        : IRequestHandler<GetTransformerClustersQuery, Response<TransformerClusterResultDto>>
    {
        // Muy por encima de los 10.000 de RNF-02: agrupar siempre recibe todos los del área.
        private const int MaxTransformers = 100_000;

        public async Task<Response<TransformerClusterResultDto>> Handle(GetTransformerClustersQuery request, CancellationToken cancellationToken)
        {
            var filter = new TransformerFilter(request.DistributorId, request.ServiceZoneId, request.Status, request.MinAvailabilityPct);
            var found = await repository.GetInBoundsAsync(
                request.MinLatitude, request.MinLongitude, request.MaxLatitude, request.MaxLongitude,
                filter, MaxTransformers, cancellationToken);

            var clusters = TransformerGrid.Aggregate(found, request.Zoom);
            var result = new TransformerClusterResultDto
            {
                Total = found.Count,
                CellSizeDegrees = TransformerGrid.CellSizeDegrees(request.Zoom),
                Items = clusters.Select(c => new TransformerClusterDto
                {
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Count = c.Count,
                    Available = c.Available,
                    Conditioned = c.Conditioned,
                    Critical = c.Critical,
                    Saturated = c.Saturated,
                    Maintenance = c.Maintenance,
                    TransformerId = c.TransformerId,
                    Code = c.Code,
                    Status = c.Status?.ToString(),
                    Color = c.Status is { } status ? TransformerMapper.ColorOf(status) : null
                }).ToList()
            };
            return new Response<TransformerClusterResultDto>(result);
        }
    }
}
