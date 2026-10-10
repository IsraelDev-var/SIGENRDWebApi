using NetTopologySuite.Geometries;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Repositories;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    /// <summary>
    /// Construcción de las consultas espaciales sobre IQueryable. Está separada del repositorio
    /// para poder comprobar el SQL generado sin una base de datos.
    /// </summary>
    public static class TransformerQueries
    {
        // SRID 4326 (WGS84): ver RN-04.
        private static readonly GeometryFactory Factory = new(new PrecisionModel(), 4326);

        public static Point ToPoint(double latitude, double longitude) => Factory.CreatePoint(new Coordinate(longitude, latitude));

        /// <summary>Anillo en sentido antihorario, como exige geography.</summary>
        public static Polygon ToEnvelope(double minLat, double minLng, double maxLat, double maxLng) =>
            Factory.CreatePolygon(new[]
            {
                new Coordinate(minLng, minLat),
                new Coordinate(maxLng, minLat),
                new Coordinate(maxLng, maxLat),
                new Coordinate(minLng, maxLat),
                new Coordinate(minLng, minLat),
            });

        public static IQueryable<Transformer> Filtered(this IQueryable<Transformer> query, TransformerFilter filter)
        {
            if (filter.DistributorId.HasValue)
                query = query.Where(t => t.DistributorId == filter.DistributorId.Value);

            if (filter.ServiceZoneId.HasValue)
                query = query.Where(t => t.ServiceZoneId == filter.ServiceZoneId.Value);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

            if (filter.MinAvailabilityPct.HasValue)
            {
                // (disponible - reservas) / total >= min, sin dividir para evitar totales en cero.
                var min = filter.MinAvailabilityPct.Value;
                query = query.Where(t => t.TotalCapacityKva > 0
                    && (t.AvailableCapacityKva - t.ReservedCapacityKva) * 100m >= min * t.TotalCapacityKva);
            }

            return query;
        }

        public static IQueryable<Transformer> InBounds(
            this IQueryable<Transformer> query,
            double minLat, double minLng, double maxLat, double maxLng,
            TransformerFilter filter, int limit)
        {
            var envelope = ToEnvelope(minLat, minLng, maxLat, maxLng);

            return query
                .Where(t => t.Location.Intersects(envelope))
                .Filtered(filter)
                .OrderBy(t => t.Code)
                .Take(limit);
        }

        public static IQueryable<Transformer> Near(
            this IQueryable<Transformer> query,
            double latitude, double longitude, double radiusMeters,
            TransformerFilter filter, int limit)
        {
            var center = ToPoint(latitude, longitude);

            return query
                .Where(t => t.Location.IsWithinDistance(center, radiusMeters))
                .Filtered(filter)
                .OrderBy(t => t.Location.Distance(center))
                .Take(limit);
        }
    }
}
