namespace GroundLayerLibrary.Interfaces
{
    public interface ITiledLayer<TType> where TType: struct
    {
        public TType GetTileType(int x, int y);
        public void SetTileType(int x, int y, TType type);
        public void SetTileTypeRange(int startX, int startY, int endX, int endY, TType type);
        public bool IsValidCoordinate(int x, int y);
        public bool CanPlaceObjectInArea(int startX, int startY, int endX, int endY);
    }
}
