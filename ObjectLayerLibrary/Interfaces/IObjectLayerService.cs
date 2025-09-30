using Core.Models;

namespace ObjectLayerLibrary.Interfaces
{
    public interface IObjectLayerService<T> where T : struct
    {
        public Task<bool> AddObjectAsync(T gameObject);

        public T? GetObjectById(string objectId);

        public  Task<bool> RemoveObjectAsync(string objectId);

        public Task<T?> GetObjectByCoordinatesAsync(int x, int y);

        public Task<List<T>> GetObjectsInAreaAsync(int x, int y, int areaWidth, int areaHeight, int count = -1);

        public Task SubscribeToEventsAsync(Func<GameObjectEvent, Task> handler);
    }
}
