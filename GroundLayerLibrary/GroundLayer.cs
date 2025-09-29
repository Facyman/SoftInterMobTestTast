using GroundLayerLibrary.Enums;
using GroundLayerLibrary.Interfaces;
using GroundLayerLibrary.Models;

namespace GroundLayerLibrary
{

    public class GroundLayer : ITiledLayer<TileTypeEnum>
    {
        private readonly Tile[] _tiles;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        public int Width { get; } // x
        public int Height { get; } // y
        public int TotalTiles => Width * Height;

        public GroundLayer(int width, int height)
        {
            if (width <= 0) throw new ArgumentException("Width must be positive", nameof(width));
            if (height <= 0) throw new ArgumentException("Height must be positive", nameof(height));

            Width = width;
            Height = height;
            _tiles = new Tile[width * height];
        }

        public GroundLayer(Tile[,] tiles) : this(tiles.GetLength(1), tiles.GetLength(0))
        {
            ArgumentNullException.ThrowIfNull(tiles);

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

        private void ValidateAreaCoordinates(int startX, int startY, int endX, int endY)
        {
            if (!IsValidCoordinate(startX, startY))
                throw new ArgumentOutOfRangeException($"Start coordinates ({startX}, {startY}) are out of bounds");
            if (!IsValidCoordinate(endX, endY))
                throw new ArgumentOutOfRangeException($"End coordinates ({endX}, {endY}) are out of bounds");
            if (startX > endX || startY > endY)
                throw new ArgumentException("Start coordinates must be less than or equal to end coordinates");
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }
    }
}