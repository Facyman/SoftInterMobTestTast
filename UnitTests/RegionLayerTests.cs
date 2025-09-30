using Core.Models;
using Core.Options;
using GroundLayerLibrary;
using Microsoft.Extensions.Options;
using Moq;
using System;

namespace UnitTests
{
    public class RegionLayerTests
    {
        private readonly GroundLayerService _service;

        public RegionLayerTests()
        {
            var mockOptions = new Mock<IOptions<AppSettings>>();
            var appSettings = new AppSettings()
            {
                MapHeight = 1000,
                MapWidth = 1000,
                Regions =
                [
                    new Region(1, "Северное королевство"),
                    new Region(2, "Южные земли"),
                    new Region(3, "Восточная империя"),
                ]
            };

            mockOptions.Setup(x => x.Value).Returns(appSettings);
            _service = new GroundLayerService(mockOptions.Object);
        }


        [Theory]
        [InlineData(1, 1)]
        [InlineData(20, 20)]
        public void GetTileRegionHeader_ReturnsNotEmptyHeader(int x, int y)
        {
            // Arrange & Act
            var header = _service.GetRegionHeader(x, y);

            // Assert
            Assert.NotNull(header.Id);
            Assert.NotEmpty(header.Name ?? "");
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(1000, 0)]
        public void GetTileRegionHeader_InvalidCoordinates_ThrowsException(int x, int y)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetRegionHeader(x, y));
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(20, 20)]
        public void GetRegionId_ReturnsNoNullUid(int x, int y)
        {
            // Arrange & Act
            var regionId = _service.GetRegionId(x, y);

            // Assert
            Assert.NotNull(regionId);
        }

        [Theory]
        [InlineData(-1, 5)]
        [InlineData(5, -1)]
        [InlineData(1000, 5)]
        public void GetRegionId_InvalidCoordinates_ThrowsException(int x, int y)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.GetRegionId(x, y));
        }


        [Theory]
        [InlineData(1, 1)]
        [InlineData(20, 20)]
        public void IsTileInRegion_TileInRegion_ReturnsTrue(int x, int y)
        {
            // Arrange
            var header = _service.GetRegionHeader(x, y);

            // Act
            bool result = _service.IsTileInRegion(x, y, header.Id!.Value);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(20, 20)]
        public void IsTileInRegion_TileNotInRegion_ReturnsFalse(int x, int y)
        {
            // Arrange
            var header = _service.GetRegionHeader(x, y);
            var random = new Random();
            int testId;
            do
            {
                testId = random.Next(1, 3);
            } 
            while (testId == header.Id!.Value);

            // Act
            bool result = _service.IsTileInRegion(x, y, (uint)testId);

            // Assert
            Assert.False(result);
        }


        [Theory]
        [InlineData(-1, 0, 1)]
        [InlineData(0, -1, 1)]
        [InlineData(1000, 0, 1)]
        public void IsTileInRegion_InvalidCoordinates_ThrowsException(int x, int y, uint regionId)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.IsTileInRegion(x, y, regionId));
        }

        [Fact]
        public void GetRegionNamesInArea_ReturnsAllRegionsInWholeMap()
        {
            // Arrange & Act - область полностью в NW регионе
            var regionNames = _service.GetRegionNamesInArea(0, 0, 999, 999);

            // Assert
            Assert.Equal(3, regionNames.Count);
            Assert.Contains("Северное королевство", regionNames);
            Assert.Contains("Южные земли", regionNames);
            Assert.Contains("Восточная империя", regionNames);
        }

        [Fact]
        public void GetRegionNamesInArea_SingleTile_ReturnsOneRegion()
        {
            // Arrange & Act
            var regionNames = _service.GetRegionNamesInArea(2, 2, 2, 2);

            // Assert
            Assert.Single(regionNames);
        }

        [Fact]
        public void GenerateRegions_RegionsDividedEqually()
        {   
            // Arrange & Act
            List<Region> regions = new List<Region>();
            for (int x = 0; x < 1000; x++)
            {
                for (int y = 0; y < 1000; y++)
                {
                    var header = _service.GetRegionHeader(x, y);
                    regions.Add(new Region(header.Id!.Value, header.Name!));
                }
            }
            var groupedRegions = regions
                .GroupBy(r => new { r.Id, r.Name })
                .Select(g => new
                {
                    RegionId = g.Key.Id,
                    RegionName = g.Key.Name,
                    Count = g.Count()
                })
                .ToList();

            // Assert
            Assert.True(groupedRegions.All(x => x.Count >= 333333));
        }
    }   
}
