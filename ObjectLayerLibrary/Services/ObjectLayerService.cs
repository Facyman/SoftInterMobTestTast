using GroundLayerLibrary.Interfaces;
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
        IObjectStoreService<GameObject> store,
        ITiledLayer tiledLayer) : IObjectLayerService
    {
        private readonly IDatabase _db = redis.GetDatabase();

        private const string GeoIndexKey = "objects:geo";
        private const string ObjectDataKeyPrefix = "object:";
        private const string ObjectEventsChannel = "object-events";
        private const int MinminalRadiusToFindPoint = 1; //km
        private const string LeftTopCorner = "lt_";
        private const string LeftBottomCorner = "lb_";
        private const string RightTopCorner = "rt_";
        private const string RightBottomCorner = "rb_";

        public async Task<bool> AddObjectAsync(GameObject gameObject)
        {
            var canPlace = tiledLayer.CanPlaceObjectInArea(gameObject.X, gameObject.Y, gameObject.X + gameObject.Width, gameObject.Y + gameObject.Height);
            if (!canPlace)
            {
                return false;
            }
            var existing = (await GetObjectsInAreaAsync(gameObject.X, gameObject.Y, gameObject.Width, gameObject.Height, 1)).Count != 0;
            if (existing)
            {
                return false;
            }

            var (ltlon, ltlat) = coordinateConverterService.TileToGeo(gameObject.X, gameObject.Y);
            var (lblon, lblat) = coordinateConverterService.TileToGeo(gameObject.X, gameObject.Y + gameObject.Height);
            var (rtlon, rtlat) = coordinateConverterService.TileToGeo(gameObject.X + gameObject.Width, gameObject.Y);
            var (rblon, rblat) = coordinateConverterService.TileToGeo(gameObject.X + gameObject.Width, gameObject.Y + gameObject.Height);
            try
            {
                await _db.GeoAddAsync(GeoIndexKey, ltlon, ltlat, LeftTopCorner + gameObject.Id);
                await _db.GeoAddAsync(GeoIndexKey, lblon, lblat, LeftBottomCorner + gameObject.Id);
                await _db.GeoAddAsync(GeoIndexKey, rtlon, rtlat, RightTopCorner + gameObject.Id);
                await _db.GeoAddAsync(GeoIndexKey, rblon, rblat, RightBottomCorner + gameObject.Id);
            }
            catch
            {
                return false;
            }
            store.AddOrUpdate(GetObjectDataKey(gameObject.Id), gameObject);
            await PublishEventAsync(GameObjectEventTypeEnum.Created, gameObject.Id);

            return true;
        }

        public GameObject? GetObjectById(string objectId)
        {
            var exists = store.TryGet(GetObjectDataKey(objectId), out var obj);
            if (!exists) return null;

            return obj;
        }

        public async Task<bool> RemoveObjectAsync(string objectId)
        {
            var existingObject = GetObjectById(objectId);
            if (existingObject == null) return false;

            try
            {
                await _db.GeoRemoveAsync(GeoIndexKey, objectId);
            }
            catch
            {
                return false;
            }

            store.Remove(GetObjectDataKey(objectId));
            await PublishEventAsync(GameObjectEventTypeEnum.Deleted, objectId);

            return true;
        }

        public async Task<GameObject?> GetObjectByCoordinates(int x, int y)
        {
            var (lon, lat) = coordinateConverterService.TileToGeo(x, y);
            var nearbyObjects = await _db.GeoSearchAsync(
                key: GeoIndexKey,
                longitude: lon,
                latitude: lat,
                shape: new GeoSearchCircle(MinminalRadiusToFindPoint, GeoUnit.Kilometers),
                count: 1,
                demandClosest: true);

            if (nearbyObjects.Length == 0)
            {
                return null;
            }
            var objectId = nearbyObjects.First().Member.ToString().Substring(3);
            return GetObjectById(objectId);
        }

        public async Task<List<GameObject>> GetObjectsInAreaAsync(int x, int y, int areaWidth, int areaHeight, int count = -1)
        {
            var result = new List<GameObject>();

            double centerX = x + areaWidth / 2;
            double centerY = y + areaHeight / 2;
            var (lon, lat) = coordinateConverterService.TileToGeo(centerX, centerY);

            var (width, height) = coordinateConverterService.GetSingleTileDimensionsInKm();

            var nearbyObjects = await _db.GeoSearchAsync(
                key: GeoIndexKey,
                longitude: lon,
                latitude: lat,
                shape: new GeoSearchBox(width * areaWidth, height * areaHeight, GeoUnit.Kilometers),
                count: count);

            foreach (var geoResult in nearbyObjects)
            {
                var objectId = geoResult.Member.ToString().Substring(3);
                var gameObject = GetObjectById(objectId);

                if (gameObject.HasValue)
                {
                    result.Add(gameObject.Value);
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