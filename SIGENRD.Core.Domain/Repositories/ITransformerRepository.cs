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

    /// <summary>Orden de la lista de gestión (B-3).</summary>
    public enum TransformerSort
    {
        /// <summary>Por código, A a Z.</summary>
        Code = 1,
        /// <summary>Menos capacidad libre primero: los que necesitan atención.</summary>
        Availability = 2,
        /// <summary>Dato más antiguo primero: los que hay que volver a informar.</summary>
        LastUpdated = 3
    }

    /// <summary>Filtros de la lista de gestión (B-3).</summary>
    public record TransformerManagementFilter(
        string? Code = null,
        int? ServiceZoneId = null,
        TransformerStatus? Status = null,
        TransformerSort Sort = TransformerSort.Code);

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

        /// <summary>Lista paginada para la gestión de la red (B-3), con zona y distribuidora.</summary>
        Task<PagedResult<Transformer>> SearchForManagementAsync(
            TransformerManagementFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<IReadOnlySet<string>> GetExistingCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);

        Task AddAsync(Transformer transformer, CancellationToken cancellationToken = default);

        Task AddRangeAsync(IEnumerable<Transformer> transformers, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ZoneRiskSummary>> GetZoneRiskAsync(CancellationToken cancellationToken = default);
    }
}
