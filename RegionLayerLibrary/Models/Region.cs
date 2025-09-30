namespace RegionLayerLibrary.Models
{
    public struct Tile(TileTypeEnum type)
    {
        public TileTypeEnum Type { get; set; } = type;
        public readonly bool CanPlaceObject => Type == TileTypeEnum.Plain;

        public
    }
}
