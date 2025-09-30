namespace ObjectLayerLibrary.Interfaces
{
    public interface ICoordinateConverterService
    {
        public (double lon, double lat) TileToGeo(double x, double y);
        public (double x, double y) GeoToTile(double lon, double lat);
        public (double width, double height) GetSingleTileDimensionsInKm();
    }
}
