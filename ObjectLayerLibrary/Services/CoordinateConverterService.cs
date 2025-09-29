using Core.Options;
using Microsoft.Extensions.Options;
using ObjectLayerLibrary.Interfaces;

namespace ObjectLayerLibrary.Services
{
    public class CoordinateConverterService(IOptions<AppSettings> options) : ICoordinateConverterService
    {
        private readonly double MapWidth = options?.Value?.MapWidth ?? throw new ArgumentNullException(nameof(MapWidth));
        private readonly double MapHeight = options?.Value?.MapHeight ?? throw new ArgumentNullException(nameof(MapHeight));

        private const double GeoMinLon = -180.0;
        private const double GeoMaxLon = 180.0;
        private const double GeoMinLat = -90.0;
        private const double GeoMaxLat = 90.0;

        public (double lon, double lat) TileToGeo(double x, double y)
        {
            double normalizedX = x / MapWidth;
            double normalizedY = y / MapHeight;

            double lon = GeoMinLon + normalizedX * (GeoMaxLon - GeoMinLon);
            double lat = GeoMaxLat - normalizedY * (GeoMaxLat - GeoMinLat);

            return (lon, lat);
        }

        public (double x, double y) GeoToTile(double lon, double lat)
        {
            double normalizedX = (lon - GeoMinLon) / (GeoMaxLon - GeoMinLon);
            double normalizedY = (GeoMaxLat - lat) / (GeoMaxLat - GeoMinLat);

            double x = normalizedX * MapWidth;
            double y = normalizedY * MapHeight;

            return (x, y);
        }

        public double CalculateGeoRadius(int tileRadius)
        {
            double normalizedRadius = tileRadius / MapWidth;
            return normalizedRadius * (GeoMaxLon - GeoMinLon);
        }
    }
}
