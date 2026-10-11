using NetTopologySuite.Geometries;

namespace SIGENRD.Core.Domain.Rules.Simulation
{
    /// <summary>Puntos WGS84 (SRID 4326) y distancias sobre la superficie terrestre.</summary>
    public static class Geo
    {
        private static readonly GeometryFactory Factory = new(new PrecisionModel(), 4326);
        private const double EarthRadiusMeters = 6371008.8;

        public static Point Point(double latitude, double longitude) => Factory.CreatePoint(new Coordinate(longitude, latitude));

        /// <summary>Distancia por la fórmula del semiverseno (haversine), en metros.</summary>
        public static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            static double Rad(double degrees) => degrees * Math.PI / 180;

            var dLat = Rad(lat2 - lat1);
            var dLng = Rad(lng2 - lng1);
            var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        }
    }
}
