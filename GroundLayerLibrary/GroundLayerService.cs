using Core.Enums;
using Core.Models;
using Core.Options;
using GroundLayerLibrary.Interfaces;
using Microsoft.Extensions.Options;

namespace GroundLayerLibrary
{

    public class GroundLayerService : ITiledLayer
    {
        private readonly int _mapWidth;
        private readonly int _mapHeight;
        private readonly Region[] _regions;
        private Tile[] _tiles;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        public int Width { get; private set; } // x
        public int Height { get; private set; } // y
        public int TotalTiles => Width * Height;

        public GroundLayerService(IOptions<AppSettings> options)
        {
            _mapWidth = options?.Value?.MapWidth ?? throw new ArgumentNullException(nameof(_mapWidth));
            _mapHeight = options?.Value?.MapHeight ?? throw new ArgumentNullException(nameof(_mapHeight));
            _regions = options.Value?.Regions ?? throw new ArgumentNullException(nameof(Region));

            if (_mapWidth <= 0) throw new ArgumentException("Width must be positive", nameof(_mapWidth));
            if (_mapHeight <= 0) throw new ArgumentException("Height must be positive", nameof(_mapHeight));

            Width = _mapWidth;
            Height = _mapHeight;

            _tiles = new Tile[Width * Height];

            GererateRandomRegions(_regions);
        }

        public void SetTilesArea(Tile[,] tiles)
        {
            ArgumentNullException.ThrowIfNull(tiles);

            Width = tiles.GetLength(1);
            Height = tiles.GetLength(0);

            _tiles = new Tile[Width * Height];

            SetTilesArea(0, 0, tiles);
        }

        public TileTypeEnum GetTileType(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");

            _lock.EnterReadLock();
            try
            {
                return _tiles[y * Width + x].Type;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void SetTileType(int x, int y, TileTypeEnum type)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");

            _lock.EnterWriteLock();
            try
            {
                _tiles[y * Width + x].Type = type;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void SetTileTypeRange(int startX, int startY, int endX, int endY, TileTypeEnum type)
        {
            ValidateAreaCoordinates(startX, startY, endX, endY);

            _lock.EnterWriteLock();
            try
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        _tiles[y * Width + x].Type = type;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private void SetTilesArea(int startX, int startY, Tile[,] tiles)
        {
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));

            int areaWidth = tiles.GetLength(1);
            int areaHeight = tiles.GetLength(0);

            if (!IsValidCoordinate(startX, startY) ||
                !IsValidCoordinate(startX + areaWidth - 1, startY + areaHeight - 1))
                throw new ArgumentOutOfRangeException("Area is out of bounds");

            _lock.EnterWriteLock();
            try
            {
                for (int y = 0; y < areaHeight; y++)
                {
                    for (int x = 0; x < areaWidth; x++)
                    {
                        _tiles[(startY + y) * Width + startX + x] = tiles[y, x];
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool CanPlaceObjectInArea(int startX, int startY, int endX, int endY)
        {
            ValidateAreaCoordinates(startX, startY, endX, endY);

            _lock.EnterReadLock();
            try
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        if (!_tiles[y * Width + x].CanPlaceObject)
                            return false;
                    }
                }
                return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public RegionHeader GetRegionHeader(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");

            var tile = _tiles[y * Width + x];
            return new RegionHeader(tile.RegionId, tile.RegionName);
        }

        public uint? GetRegionId(int x, int y)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");
            
            return _tiles[y * Width + x].RegionId;
        }

        public bool IsTileInRegion(int x, int y, uint regionId)
        {
            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds");

            return _tiles[y * Width + x].RegionId == regionId;
        }
        public HashSet<string> GetRegionNamesInArea(int xStart, int yStart, int xEnd, int yEnd)
        {
            var regionNames = new HashSet<string>();

            for (int y = yStart; y <= yEnd && y < Height; y++)
            {
                for (int x = xStart; x <= xEnd && x < Width; x++)
                {
                    var tile = _tiles[y * Width + x];
                    if (tile.RegionName != null)
                    {
                        regionNames.Add(tile.RegionName);
                    }
                }
            }

            return regionNames;
        }

        private void ValidateAreaCoordinates(int startX, int startY, int endX, int endY)
        {
            if (!IsValidCoordinate(startX, startY))
                throw new ArgumentOutOfRangeException($"Start coordinates ({startX}, {startY}) are out of bounds");
            if (!IsValidCoordinate(endX, endY))
                throw new ArgumentOutOfRangeException($"End coordinates ({endX}, {endY}) are out of bounds");
            if (startX > endX || startY > endY)
                throw new ArgumentException("Start coordinates must be less than or equal to end coordinates");
        }

        private void GererateRandomRegions(Region[] regions)
        {
            if (regions.Length == 0)
                throw new ArgumentNullException($"Region list is empty");

            int totalRegions = regions.Length;
            int totalTiles = Width * Height;
            var random = new Random();

            var regionAssignments = new int[totalTiles];

            int tilesPerRegion = totalTiles / totalRegions;
            int rest = totalTiles % totalRegions;

            for (int regPos = 0; regPos < regions.Length; regPos++)
            {
                for (int j = 0 + regPos * tilesPerRegion; j < totalTiles; j++)
                {
                    regionAssignments[j] = regPos;
                }
            }

            for (int i = totalTiles - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (regionAssignments[i], regionAssignments[j]) = (regionAssignments[j], regionAssignments[i]);
            }

            for (int i = 0; i < totalTiles; i++)
            {
                var existingTile = _tiles[i];
                _tiles[i].RegionId = regions[regionAssignments[i]].Id;
                _tiles[i].RegionName = regions[regionAssignments[i]].Name;
            }
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }
    }
}