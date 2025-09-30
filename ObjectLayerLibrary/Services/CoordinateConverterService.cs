using Core.Options;
using Microsoft.Extensions.Options;
using ObjectLayerLibrary.Interfaces;

namespace ObjectLayerLibrary.Services
{
    public class CoordinateConverterService(IOptions<AppSettings> options) : ICoordinateConverterService
    {
        private readonly double MapWidth = options?.Value?.MapWidth ?? throw new ArgumentNullException(nameof(MapWidth));
        private readonly double MapHeight = options?.Value?.MapHeight ?? throw new ArgumentNullException(nameof(MapHeight));

        private const double GeoMinLon = 0.0;
        private const double GeoMaxLon = 100.0;
        private const double GeoMinLat = 0.0;
        private const double GeoMaxLat = 100.0;
        private const double OneDegreeInKm = 100; //Берем меньше базовой дистанции(111км) чтобы не задевать не входящие объекты;

        public (double lon, double lat) TileToGeo(double x, double y)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");

            double normalizedX = x / MapWidth;
            double normalizedY = y / MapHeight;

            double lon = GeoMinLon + normalizedX * (GeoMaxLon - GeoMinLon);
            double lat = GeoMinLat + normalizedY * (GeoMaxLat - GeoMinLat);

            return (lon, lat);
        }

        public (double x, double y) GeoToTile(double lon, double lat)
        {
            double normalizedX = (lon - GeoMinLon) / (GeoMaxLon - GeoMinLon);
            double normalizedY = (lat - GeoMinLat) / (GeoMaxLat - GeoMinLat);

            double x = normalizedX * MapWidth;
            double y = normalizedY * MapHeight;

            return (x, y);
        }

        public (double width, double height) GetSingleTileDimensionsInKm()
        {
            double singleTileLon = (GeoMaxLon - GeoMinLon)/MapWidth;
            double singleTileLat = (GeoMaxLon - GeoMinLon)/MapHeight;

            double width = singleTileLon * OneDegreeInKm;
            double height = singleTileLat * OneDegreeInKm;

            return (width, height);
        }

        public bool IsValidCoordinate(double x, double y)
        {
            return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
        }
    }
}
