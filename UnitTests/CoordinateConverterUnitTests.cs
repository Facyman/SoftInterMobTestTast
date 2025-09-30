using Core.Options;
using Microsoft.Extensions.Options;
using Moq;
using ObjectLayerLibrary.Services;

namespace UnitTests
{
    public class CoordinateConverterServiceTests
    {
        [Fact]
        public void GeoToTile_ShouldReturnValidValue()
        {
            // Arrange
            var mockOptions = new Mock<IOptions<AppSettings>>();
            var appSettings = new AppSettings { MapWidth = 1000, MapHeight = 1000 };
            mockOptions.Setup(x => x.Value).Returns(appSettings);
            var service = new CoordinateConverterService(mockOptions.Object);

            // Act
            var geo = service.TileToGeo(50, 50);

            // Asset
            Assert.Equal((5,5), geo);
        }

        [Fact]
        public void TileToGeo_ShouldReturnValidValue()
        {
            // Arrange
            var appSettings = new AppSettings { MapWidth = 1000, MapHeight = 1000 };
            var mockOptions = new Mock<IOptions<AppSettings>>();
            mockOptions.Setup(x => x.Value).Returns(appSettings);
            var service = new CoordinateConverterService(mockOptions.Object);

            // Act
            var tile = service.GeoToTile(5, 5);

            // Asset
            Assert.Equal((50, 50), tile);
        }
    }
}