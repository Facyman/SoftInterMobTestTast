using Core.Models;
using Core.Options;
using GroundLayerLibrary;
using Microsoft.Extensions.Options;
using Moq;
using ObjectLayerLibrary.Services;
using StackExchange.Redis;


namespace UnitTests
{
    public class ObjectLayerServiceTests
    {
        private readonly ObjectLayerService _service;

        public ObjectLayerServiceTests()
        {
            var redisConnectionString = "localhost:6379";
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

            var redis = ConnectionMultiplexer.Connect(redisConnectionString);
            var coordinatesService = new CoordinateConverterService(mockOptions.Object);
            var groundLayerService = new GroundLayerService(mockOptions.Object);
            var storeService = new GameObjectStoreService();
            _service = new ObjectLayerService(redis, coordinatesService, storeService, groundLayerService);
        }

        [Fact]
        public async Task AddObject_ValidObject_StoresInRedis()
        {
            // Arrange
            var gameObject = new GameObject("test-1", 10, 10, 3, 2);

            // Act
            var result = await _service.AddObjectAsync(gameObject);

            // Assert
            var retrieved = _service.GetObjectById("test-1");
            Assert.NotNull(retrieved);
        }

        [Fact]
        public async Task GetObjectAt_ObjectExists_ReturnsObject()
        {
            // Arrange
            var gameObject = new GameObject("test-2", 50, 50, 4, 4);
            await _service.AddObjectAsync(gameObject);

            // Act
            var result = await _service.GetObjectByCoordinates(50, 50);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-2", result?.Id);
        }

        [Fact]
        public async Task RemoveObject_ObjectExists_RemovesFromRedis()
        {
            // Arrange
            var gameObject = new GameObject("test-3", 50, 50, 4, 4);
            await _service.AddObjectAsync(gameObject);

            // Act
            var removeResult = await _service.RemoveObjectAsync("test-3");
            var retrieved = _service.GetObjectById("test-3");

            // Assert
            Assert.True(removeResult);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task GetObjectsInArea_ReturnsIntersectingObjects()
        {
            // Arrange
            var obj1 = new GameObject("area-1", 10, 10, 5, 5);
            var obj2 = new GameObject("area-2", 20, 20, 5, 5);
            await _service.AddObjectAsync(obj1);
            await _service.AddObjectAsync(obj2);

            // Act
            var objectsInArea = await _service.GetObjectsInAreaAsync(12, 12, 5, 5);

            // Assert
            Assert.Single(objectsInArea); // Только obj1 должен попасть в область
            Assert.Contains(objectsInArea, o => o.Id == "area-1");
        }

        [Fact]
        public async Task ObjectEvents_AreRaisedOnOperations()
        {
            // Arrange
            var gameObject = new GameObject("event-1", 20, 20, 5, 5);
            var eventCompleted = new TaskCompletionSource<bool>();
            var eventHappened = false;
            _ = _service.SubscribeToEventsAsync(async (@event) =>
            {
                eventCompleted.SetResult(true);
                await Task.FromResult(true);
            });

            // Act
            await _service.AddObjectAsync(gameObject);

            // Assert
            // Wait for event with 1 second timeout
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(eventCompleted.Task, timeoutTask);

            // Assert
            Assert.True(completedTask == eventCompleted.Task, "Event was not triggered within 5 second");
            Assert.True(eventCompleted.Task.Result);
        }
    }
}