using Core.Options;
using Microsoft.Extensions.Options;
using Moq;
using ObjectLayerLibrary.Services;

namespace UnitTests
{
    public class CoordinateConverterServiceTests
    {
        [Fact]
        public void AllMethods_WithExtremeMapDimensions_HandleCorrectly()
        {
            // Arrange - Very large map dimensions
            var mockOptions = new Mock<IOptions<AppSettings>>();
            var appSettings = new AppSettings { MapWidth = 1000000, MapHeight = 500000 };
            mockOptions.Setup(x => x.Value).Returns(appSettings);
            var service = new CoordinateConverterService(mockOptions.Object);

            // Act & Assert - Should not throw and should calculate correctly
            var geo = service.TileToGeo(500000, 250000);
            var tile = service.GeoToTile(50.0, 50.0);
            var dimensions = service.GetSingleTileDimensionsInKm();

            Assert.NotNull(geo);
            Assert.NotNull(tile);
            Assert.True(dimensions.width > 0);
            Assert.True(dimensions.height > 0);
        }

        [Fact]
        public void Service_WithMinimumValidMapDimensions_WorksCorrectly()
        {
            // Arrange - Minimum valid dimensions
            var appSettings = new AppSettings { MapWidth = 1, MapHeight = 1 };
            var mockOptions = new Mock<IOptions<AppSettings>>();
            mockOptions.Setup(x => x.Value).Returns(appSettings);
            var service = new CoordinateConverterService(mockOptions.Object);

            // Act & Assert - Should not throw for valid coordinate
            Assert.NotNull(service.TileToGeo(0, 0));
            Assert.NotNull(service.GeoToTile(50.0, 50.0));

            // Should throw for invalid coordinate
            Assert.Throws<ArgumentOutOfRangeException>(() => service.TileToGeo(1, 1));
        }
    }
}