using GroundLayerLibrary.Enums;

namespace GroundLayerLibrary.Models
{
    public struct Tile(TileTypeEnum type)
    {
        public TileTypeEnum Type { get; set; } = type;
        public readonly bool CanPlaceObject => Type == TileTypeEnum.Plain;

        public override int GetHashCode() => Type.GetHashCode();
        public override string ToString() => Type.ToString();
    }
}
