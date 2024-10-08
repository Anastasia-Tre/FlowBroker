﻿using System.Collections.Concurrent;

namespace FlowBroker.Core.PathMatching;

public class CachedPathMatcher : IPathMatcher
{
    private readonly ConcurrentDictionary<(string, string), CacheItem> _cache =
        new();

    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    private readonly int _maxCacheSize = 1000;

    public bool Match(string flowPacketPath, string flowPath)
    {
        if (string.IsNullOrEmpty(flowPacketPath) ||
            string.IsNullOrEmpty(flowPath))
            return false;

        var cacheKey = (flowPacketPath, flowPath);
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(cacheKey, out var cachedItem))
        {
            if (now - cachedItem.CacheTime < _cacheTtl)
                return cachedItem.Result;
            _cache.TryRemove(cacheKey, out _);
        }

        var result = MatchPaths(flowPacketPath, flowPath);
        _cache[(flowPacketPath, flowPath)] = new CacheItem(result, now);

        CheckCacheSize();
        return result;
    }

    private void CheckCacheSize()
    {
        if (_cache.Count > _maxCacheSize)
        {
            var oldestEntries = _cache.OrderBy(pair => pair.Value.CacheTime)
                .Take(_cache.Count - _maxCacheSize);

            foreach (var entry in oldestEntries)
                _cache.TryRemove(entry.Key, out _);
        }
    }

    private bool MatchPaths(string flowPacketPath, string flowPath)
    {
        var flowPacketPathSegments =
            flowPacketPath.Split(IPathMatcher.PathSeparator);
        var queuePathSegments = flowPath.Split(IPathMatcher.PathSeparator);

        var minSegmentCount = Math.Min(flowPacketPathSegments.Length,
            queuePathSegments.Length);

        for (var i = 0; i < minSegmentCount; i++)
        {
            var flowPacketSegment = flowPacketPathSegments[i];
            var queueSegment = queuePathSegments[i];

            if (flowPacketSegment == IPathMatcher.WildCard ||
                queueSegment == IPathMatcher.WildCard)
                continue;

            if (flowPacketSegment == queueSegment)
                continue;

            return false;
        }

        return true;
    }
}

internal class CacheItem
{
    public CacheItem(bool result, DateTime cacheTime)
    {
        Result = result;
        CacheTime = cacheTime;
    }

    public bool Result { get; }
    public DateTime CacheTime { get; }
}
