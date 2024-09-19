using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Utils.Pooling;

public interface IPooledObject
{
    public Guid PoolId { get; set; }
}

public interface IObjectPool
{
    T Get<T>() where T : IPooledObject, new();
    void Return<T>(T o) where T : IPooledObject;
}

public class ObjectPool : IObjectPool
{
    public static readonly ObjectPool Shared = new();

    private readonly Dictionary<int, Queue<object>> _objectTypeDict;

    public ObjectPool()
    {
        _objectTypeDict = new Dictionary<int, Queue<object>>();
    }

    public T Get<T>() where T : IPooledObject, new()
    {
        var type = typeof(T);
        var typeKey = type.Name.GetHashCode();

        lock (_objectTypeDict)
        {
            if (!_objectTypeDict.ContainsKey(typeKey))
            {
                _objectTypeDict[typeKey] = new Queue<object>();
            }

            var bag = _objectTypeDict[typeKey];

            if (bag.TryDequeue(out var o))
            {
                var i = (T)o;
                return i;
            }

            var newInstance = new T { PoolId = Guid.NewGuid() };
            return newInstance;
        }
    }

    public void Return<T>(T o) where T : IPooledObject
    {
        var type = typeof(T);
        var typeKey = type.Name.GetHashCode();

        lock (_objectTypeDict)
        {
            _objectTypeDict[typeKey].Enqueue(o);
        }
    }
}