using Core.Enums;

namespace GroundLayerLibrary.Interfaces
{
    public interface ITiledLayerService
    {
        public TileTypeEnum GetTileType(int x, int y);
        public void SetTileType(int x, int y, TileTypeEnum type);
        public void SetTileTypeRange(int startX, int startY, int endX, int endY, TileTypeEnum type);
        public bool IsValidCoordinate(int x, int y);
        public bool CanPlaceObjectInArea(int startX, int startY, int endX, int endY);
    }
}
