using Core.Options;
using GroundLayerLibrary;
using GroundLayerLibrary.Enums;
using GroundLayerLibrary.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests
{
    public class GroundLayerTests
    {
        private readonly GroundLayerService _service;

        public GroundLayerTests()
        {
            var mockOptions = new Mock<IOptions<AppSettings>>();
            var appSettings = new AppSettings()
            {
                MapHeight = 1000,
                MapWidth = 1000,
            };

            mockOptions.Setup(x => x.Value).Returns(appSettings);
            _service = new GroundLayerService(mockOptions.Object);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(1000, 0)]
        [InlineData(0, 1000)]
        [InlineData(10000, 10000)]
        public void GetTileType_InvalidCoordinates_ThrowsException(int x, int y)
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetTileType(x, y));
        }

        [Fact]
        public void SetTileType_SetsCorrectTileType()
        {
            // Arrange
            _service.SetTileType(5, 5, TileTypeEnum.Mountain);

            // Act
            var type = _service.GetTileType(5, 5);

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

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.SetTileType(x, y, TileTypeEnum.Plain));
        }

        [Fact]
        public void SetTileType_OverwritesPreviousValue()
        {
            // Arrange
            _service.SetTileType(1, 1, TileTypeEnum.Plain);

            // Act
            _service.SetTileType(1, 1, TileTypeEnum.Mountain);

            // Assert
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(1, 1));
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
            _service.SetTilesArea(tiles);

            // Assert
            Assert.Equal(3, _service.Width);
            Assert.Equal(2, _service.Height);
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(0, 0));
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(1, 0));
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(1, 1));
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(2, 1));
        }

        [Fact]
        public void SetTileTypeRange_ValidArea_FillsCorrectTiles()
        {
            // Arrange

            // Act
            _service.SetTileTypeRange(1, 1, 3, 3, TileTypeEnum.Mountain);

            // Assert
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(0, 0));
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(4, 4));
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(0, 2));
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(2, 0));

            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(1, 1));
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(3, 3));
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(2, 2));
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(1, 3));
        }

        [Fact]
        public void SetTileTypeRange_SingleTile_FillsSingleTile()
        {
            // Arrange

            // Act
            _service.SetTileTypeRange(1, 1, 1, 1, TileTypeEnum.Mountain);

            // Assert
            Assert.Equal(TileTypeEnum.Mountain, _service.GetTileType(1, 1));
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(0, 0));
            Assert.Equal(TileTypeEnum.Plain, _service.GetTileType(2, 2));
        }

        [Theory]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 0, 1000, 1)]
        [InlineData(0, 0, 1, 1000)]
        [InlineData(1000, 2, 1, 1)]
        public void SetTileTypeRange_InvalidCoordinates_ThrowsException(int startX, int startY, int endX, int endY)
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.SetTileTypeRange(startX, startY, endX, endY, TileTypeEnum.Plain));
        }

        [Fact]
        public void CanPlaceObjectInArea_AllPlains_ReturnsTrue()
        {
            // Arrange

            // Act
            bool result = _service.CanPlaceObjectInArea(0, 0, 2, 2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_ContainsMountains_ReturnsFalse()
        {
            // Arrange
            _service.SetTileType(1, 1, TileTypeEnum.Mountain);

            // Act
            bool result = _service.CanPlaceObjectInArea(0, 0, 2, 2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_MountainOnBorder_ReturnsFalse()
        {
            // Arrange
            _service.SetTileType(0, 0, TileTypeEnum.Mountain);

            // Act
            bool result = _service.CanPlaceObjectInArea(0, 0, 2, 2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_SinglePlainTile_ReturnsTrue()
        {
            // Arrange
            _service.SetTileTypeRange(0, 0, 2, 2, TileTypeEnum.Mountain);
            _service.SetTileType(1, 1, TileTypeEnum.Plain);

            // Act
            bool result = _service.CanPlaceObjectInArea(1, 1, 1, 1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_SingleMountainTile_ReturnsFalse()
        {
            // Arrange

            // Act
            bool result = _service.CanPlaceObjectInArea(1, 1, 1, 1);

            // Assert
            Assert.True(result);

            // Устанавливаем Mountain и проверяем снова
            _service.SetTileType(1, 1, TileTypeEnum.Mountain);
            result = _service.CanPlaceObjectInArea(1, 1, 1, 1);

            Assert.False(result);
        }

        [Fact]
        public void CanPlaceObjectInArea_PartialAreaAllPlains_ReturnsTrue()
        {
            // Arrange
            _service.SetTileTypeRange(0, 0, 4, 4, TileTypeEnum.Mountain);
            _service.SetTileTypeRange(1, 1, 3, 3, TileTypeEnum.Plain);

            // Act
            bool result = _service.CanPlaceObjectInArea(1, 1, 3, 3);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 0, 1000, 1)]
        [InlineData(0, 0, 1, 1000)]
        public void CanPlaceObjectInArea_InvalidCoordinates_ThrowsException(int startX, int startY, int endX, int endY)
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.CanPlaceObjectInArea(startX, startY, endX, endY));
        }

        [Fact]
        public void CanPlaceObjectInArea_StartGreaterThanEnd_ThrowsException()
        {
            // Arrange

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CanPlaceObjectInArea(2, 2, 1, 1));
        }
    }
}