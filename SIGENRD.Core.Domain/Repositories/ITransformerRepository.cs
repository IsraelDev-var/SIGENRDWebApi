using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Repositories
{
    /// <summary>Filtros comunes del mapa (RF-03).</summary>
    public record TransformerFilter(
        int? DistributorId = null,
        int? ServiceZoneId = null,
        TransformerStatus? Status = null,
        decimal? MinAvailabilityPct = null);

    /// <summary>Resumen de riesgo por zona (RF-29).</summary>
    public record ZoneRiskSummary(int ServiceZoneId, string ZoneName, int Total, int Critical, int Saturated);

    /// <summary>
    /// Repositorio específico de transformadores: las consultas espaciales no caben en el repositorio genérico.
    /// </summary>
    public interface ITransformerRepository
    {
        /// <summary>Transformadores dentro de un rectángulo, ordenados por código.</summary>
        Task<IReadOnlyList<Transformer>> GetInBoundsAsync(
            double minLatitude, double minLongitude, double maxLatitude, double maxLongitude,
            TransformerFilter filter, int limit, CancellationToken cancellationToken = default);

        /// <summary>Transformadores dentro de un radio en metros, del más cercano al más lejano.</summary>
        Task<IReadOnlyList<Transformer>> GetNearAsync(
            double latitude, double longitude, double radiusMeters,
            TransformerFilter filter, int limit, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Transformer>> SearchByCodeAsync(string term, int limit, CancellationToken cancellationToken = default);

        /// <summary>Incluye distribuidora y zona. Con asTracking=true se puede modificar y guardar.</summary>
        Task<Transformer?> GetByIdAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default);

        Task<IReadOnlySet<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);

        Task AddAsync(Transformer transformer, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<Transformer> transformers, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ZoneRiskSummary>> GetZoneRiskAsync(CancellationToken cancellationToken = default);
    }
}
