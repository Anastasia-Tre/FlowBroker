﻿using System.Collections.Concurrent;

namespace FlowBroker.Core.Utils.Persistence;

public interface IDataRepository<in TKey, TValue> where TKey : notnull
{
    void Setup();
    void Add(TKey key, TValue value);
    bool Remove(TKey key);
    bool TryGet(TKey key, out TValue value);
    IEnumerable<TValue> GetAll();
}

public abstract class
    DataRepository<TKey, TValue> : IDataRepository<TKey, TValue>
    where TKey : notnull
{
    protected readonly ConcurrentDictionary<TKey, TValue> Data = new();

    public virtual void Setup()
    {
    }

    public virtual void Add(TKey key, TValue value)
    {
        if (!Data.TryAdd(key, value))
            throw new ArgumentException(
                $"An item with the key {key} already exists.");
    }

    public virtual bool Remove(TKey key)
    {
        return Data.Remove(key, out _);
    }

    public virtual bool TryGet(TKey key, out TValue value)
    {
        return Data.TryGetValue(key, out value);
    }

    public virtual IEnumerable<TValue> GetAll()
    {
        return Data.Values;
    }
}
