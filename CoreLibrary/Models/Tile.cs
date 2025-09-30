using Core.Enums;

namespace Core.Models
{
    public struct Tile(TileTypeEnum type)
    {
        public TileTypeEnum Type { get; set; } = type;
        public readonly bool CanPlaceObject => Type == TileTypeEnum.Plain;
        public uint? RegionId { get; set; }
        public string? RegionName { get; set; }
    }
}
