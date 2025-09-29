using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectLayerLibrary.Interfaces
{
    public interface IObjectStoreService<T>
    {
        public void AddOrUpdate(string key, T obj);

        public bool TryGet(string key, out T obj);

        public bool Remove(string key);
    }
}
