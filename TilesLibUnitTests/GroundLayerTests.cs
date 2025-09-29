using BenchmarkDotNet.Attributes;
using TIlesLib;
using TIlesLib.Enums;

namespace TileMapSystem.Tests
{
    public class GroundLayerTests
    {
        private const int TestWidth = 1000;
        private const int TestHeight = 1000;

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(1000, 0)]
        [InlineData(0, 1000)]
        [InlineData(10000, 10000)]
        public void GetTileType_InvalidCoordinates_ThrowsException(int x, int y)
        {
            // Arrange
            var layer = new GroundLayer(TestWidth, TestHeight);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetTileType(x, y));
        }

        [Fact]
        public void SetTileType_SetsCorrectTileType()
        {
            // Arrange
            var layer = new GroundLayer(TestWidth, TestHeight);
            layer.SetTileType(5, 5, TileTypeEnum.Mountain);

            // Act
            var type = layer.GetTileType(5, 5);

            // Assert
            Assert.Equal(TileTypeEnum.Mountain, type);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(1000, 0)]
        [InlineData(0, 1000)]
        public void SetTileType_InvalidCoordinates_ThrowsException(int x, int y)
        {
            // Arrange
            var layer = new GroundLayer(TestWidth, TestHeight);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetTileType(x, y, TileTypeEnum.Plain));
        }

        [Fact]
        public void SetTileType_OverwritesPreviousValue()
        {
            // Arrange
            var layer = new GroundLayer(TestWidth, TestHeight);
            layer.SetTileType(1, 1, TileTypeEnum.Plain);

            // Act
            layer.SetTileType(1, 1, TileTypeEnum.Mountain);

            // Assert
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(1, 1));
        }

        [Fact]
        public void FromCollection_2DArray_CreatesCorrectLayer()
        {
            // Arrange
            var tiles = new Tile[2, 3]
            {
                { new Tile(TileTypeEnum.Plain), new Tile(TileTypeEnum.Mountain), new Tile(TileTypeEnum.Plain) },
                { new Tile(TileTypeEnum.Mountain), new Tile(TileTypeEnum.Plain), new Tile(TileTypeEnum.Mountain) }
            };

            // Act
            var layer = new GroundLayer(tiles);

            // Assert
            Assert.Equal(3, layer.Width);
            Assert.Equal(2, layer.Height);
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(0, 0));
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(1, 0));
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(1, 1));
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(2, 1));
        }

        [Fact]
        public void SetTileTypeRange_ValidArea_FillsCorrectTiles()
        {
            // Arrange
            var layer = new GroundLayer(5, 5);

            // Act
            layer.SetTileTypeRange(1, 1, 3, 3, TileTypeEnum.Mountain);

            // Assert
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(0, 0));
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(4, 4));
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(0, 2));
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(2, 0));

            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(1, 1));
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(3, 3));
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(2, 2));
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(1, 3));
        }

        [Fact]
        public void SetTileTypeRange_SingleTile_FillsSingleTile()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);

            // Act
            layer.SetTileTypeRange(1, 1, 1, 1, TileTypeEnum.Mountain);

            // Assert
            Assert.Equal(TileTypeEnum.Mountain, layer.GetTileType(1, 1));
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(0, 0));
            Assert.Equal(TileTypeEnum.Plain, layer.GetTileType(2, 2));
        }

        [Theory]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 0, 10, 1)]
        [InlineData(0, 0, 1, 10)]
        [InlineData(3, 2, 1, 1)]
        public void SetTileTypeRange_InvalidCoordinates_ThrowsException(int startX, int startY, int endX, int endY)
        {
            // Arrange
            var layer = new GroundLayer(3, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetTileTypeRange(startX, startY, endX, endY, TileTypeEnum.Plain));
        }

        [Fact]
        public void CanPlaceObjectInArea_AllPlains_ReturnsTrue()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);

            // Act
            bool result = layer.CanPlaceObjectInArea(0, 0, 2, 2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_ContainsMountains_ReturnsFalse()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);
            layer.SetTileType(1, 1, TileTypeEnum.Mountain);

            // Act
            bool result = layer.CanPlaceObjectInArea(0, 0, 2, 2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_MountainOnBorder_ReturnsFalse()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);
            layer.SetTileType(0, 0, TileTypeEnum.Mountain);

            // Act
            bool result = layer.CanPlaceObjectInArea(0, 0, 2, 2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_SinglePlainTile_ReturnsTrue()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);
            layer.SetTileTypeRange(0, 0, 2, 2, TileTypeEnum.Mountain);
            layer.SetTileType(1, 1, TileTypeEnum.Plain);

            // Act
            bool result = layer.CanPlaceObjectInArea(1, 1, 1, 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_SingleMountainTile_ReturnsFalse()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);

            // Act
            bool result = layer.CanPlaceObjectInArea(1, 1, 1, 1);

            // Assert
            Assert.True(result);

            // Устанавливаем Mountain и проверяем снова
            layer.SetTileType(1, 1, TileTypeEnum.Mountain);
            result = layer.CanPlaceObjectInArea(1, 1, 1, 1);

            Assert.False(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_PartialAreaAllPlains_ReturnsTrue()
        {
            // Arrange
            var layer = new GroundLayer(5, 5);
            layer.SetTileTypeRange(0, 0, 4, 4, TileTypeEnum.Mountain);
            layer.SetTileTypeRange(1, 1, 3, 3, TileTypeEnum.Plain);

            // Act
            bool result = layer.CanPlaceObjectInArea(1, 1, 3, 3);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 0, 5, 1)]
        [InlineData(0, 0, 1, 5)]
        public void CanPlaceObjectInArea_InvalidCoordinates_ThrowsException(int startX, int startY, int endX, int endY)
        {
            // Arrange
            var layer = new GroundLayer(3, 3);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => layer.CanPlaceObjectInArea(startX, startY, endX, endY));
        }

        [Fact]
        public void CanPlaceObjectInArea_StartGreaterThanEnd_ThrowsException()
        {
            // Arrange
            var layer = new GroundLayer(3, 3);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => layer.CanPlaceObjectInArea(2, 2, 1, 1));
        }
    }
}