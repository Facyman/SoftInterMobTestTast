using ObjectLayerLibrary.Enums;
using ObjectLayerLibrary.Interfaces;
using ObjectLayerLibrary.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace ObjectLayerLibrary.Services
{
    public class ObjectLayerService(
        IConnectionMultiplexer redis,
        ICoordinateConverterService coordinateConverterService,
        IObjectStoreService<GameObject> store)
    {
        private readonly IDatabase _db = redis.GetDatabase();

        private const string GeoIndexKey = "objects:geo";
        private const string ObjectDataKeyPrefix = "object:";
        private const string ObjectEventsChannel = "object-events";

        public async Task<bool> AddObjectAsync(GameObject gameObject)
        {
            double centerX = gameObject.X + gameObject.Width / 2.0;
            double centerY = gameObject.Y + gameObject.Height / 2.0;
            var coordinates = coordinateConverterService.TileToGeo(centerX, centerY);
            try
            {
                await _db.GeoAddAsync(GeoIndexKey, coordinates.lon, coordinates.lat, gameObject.Id);
            }
            catch
            {
                return false;
            }
            store.AddOrUpdate(GetObjectDataKey(gameObject.Id), gameObject);
            await PublishEventAsync(GameObjectEventTypeEnum.Created, gameObject.Id);

            return true;
        }

        public GameObject? GetObjectByIdAsync(string objectId)
        {
            var exists = store.TryGet(GetObjectDataKey(objectId), out var obj);
            if (!exists) return null;

            return obj;
        }

        public async Task<bool> RemoveObjectAsync(string objectId)
        {
            var existingObject = GetObjectByIdAsync(objectId);
            if (existingObject == null) return false;

            try
            {
                await _db.GeoRemoveAsync(GeoIndexKey, objectId);
            }
            catch
            {
                store.Remove(GetObjectDataKey(objectId));
                await PublishEventAsync(GameObjectEventTypeEnum.Deleted, objectId);
                return false;
            }

            return true;
        }

        public async Task<bool> IsObjectInAreaAsync(string objectId, int areaX, int areaY, int areaWidth, int areaHeight)
        {
            var gameObject = GetObjectByIdAsync(objectId);
            if (gameObject == null) return false;

            return ((GameObject)gameObject).IntersectsWith(areaX, areaY, areaWidth, areaHeight);
        }

        public async Task<List<GameObject>> GetObjectsInAreaAsync(int areaX, int areaY, int areaWidth, int areaHeight)
        {
            var result = new List<GameObject>();

            double centerX = areaX + areaWidth / 2.0;
            double centerY = areaY + areaHeight / 2.0;
            var (centerLon, centerLat) = coordinateConverterService.TileToGeo(centerX, centerY);

            double diagonal = Math.Sqrt(areaWidth * areaWidth + areaHeight * areaHeight);
            double searchRadius = coordinateConverterService.CalculateGeoRadius((int)diagonal + 10);

            var nearbyObjects = await _db.GeoRadiusAsync(
                GeoIndexKey, centerLon, centerLat, searchRadius, GeoUnit.Meters);

            foreach (var geoResult in nearbyObjects)
            {
                var objectId = geoResult.Member.ToString();
                var gameObject = GetObjectByIdAsync(objectId);

                if (gameObject != null && ((GameObject)gameObject).IntersectsWith(areaX, areaY, areaWidth, areaHeight))
                {
                    result.Add((GameObject)gameObject);
                }
            }

            return result;
        }

        public async Task SubscribeToEventsAsync(Func<GameObjectEvent, Task> handler)
        {
            var subscriber = redis.GetSubscriber();

            await subscriber.SubscribeAsync(ObjectEventsChannel, async (channel, message) =>
            {
                try
                {
                    var gameEvent = JsonSerializer.Deserialize<GameObjectEvent>(message!);
                    if (gameEvent != null)
                    {
                        await handler(gameEvent);
                    }
                }
                catch (Exception ex)
                {
                }
            });
        }

        private async Task PublishEventAsync(GameObjectEventTypeEnum eventType, string id)
        {
            var gameEvent = new GameObjectEvent(
                id,
                eventType,
                DateTime.UtcNow
            );

            var serializedEvent = JsonSerializer.Serialize(gameEvent);
            var subscriber = redis.GetSubscriber();
            await subscriber.PublishAsync(ObjectEventsChannel, serializedEvent);
        }

        private static string GetObjectDataKey(string objectId) => $"{ObjectDataKeyPrefix}{objectId}";
    }
}