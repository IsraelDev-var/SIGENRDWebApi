

namespace SIGENRD.Core.Domain.ValueObjects
{
    public class GeoCoordinate
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        private GeoCoordinate() { } // for EF

        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString() => $"{Latitude}, {Longitude}";
    }
}
