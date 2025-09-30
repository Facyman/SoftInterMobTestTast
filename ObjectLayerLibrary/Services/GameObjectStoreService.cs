using Core.Models;
using ObjectLayerLibrary.Interfaces;
using System.Collections.Concurrent;

namespace ObjectLayerLibrary.Services
{
    public class GameObjectStoreService : IObjectStoreService<GameObject>
    {
        private readonly ConcurrentDictionary<string, GameObject> _objects = new();
        public void AddOrUpdate(string key, GameObject obj)
        {
            _objects[key] = obj;
        }

        public bool TryGet(string key, out GameObject obj)
        {
            return _objects.TryGetValue(key, out obj);
        }

        public bool Remove(string key)
        {
            return _objects.TryRemove(key, out _);
        }
    }
}
