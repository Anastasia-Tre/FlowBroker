using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlowBroker.Core.PathMatching;

public class CachedRegexPathMatcher : IPathMatcher
{
    private readonly ConcurrentDictionary<(string, string), CacheItem> _cache = new();

    private readonly int _maxCacheSize = 1000;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

    private void CheckCacheSize()
    {
        if (_cache.Count > _maxCacheSize)
        {
            var oldestEntries = _cache.OrderBy(pair => pair.Value.CacheTime)
                                      .Take(_cache.Count - _maxCacheSize);

            foreach (var entry in oldestEntries)
            {
                _cache.TryRemove(entry.Key, out _);
            }
        }
    }

    public bool Match(string flowPacketPath, string flowPath)
    {
        if (string.IsNullOrEmpty(flowPacketPath) || string.IsNullOrEmpty(flowPath))
            return false;

        var cacheKey = (flowPacketPath, flowPath);
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(cacheKey, out var cachedItem))
        {
            if (now - cachedItem.CacheTime < _cacheTtl)
                return cachedItem.Result;
            else
                _cache.TryRemove(cacheKey, out _);
        }

        var result = MatchPaths(flowPacketPath, flowPath);
        _cache[(flowPacketPath, flowPath)] = new CacheItem(result, now);

        CheckCacheSize();
        return result;
    }

    private bool MatchPaths(string flowPacketPath, string flowPath)
    {
        var flowPacketPathSegments = flowPacketPath.Split(IPathMatcher.PathSeparator);
        var flowPathSegments = flowPath.Split(IPathMatcher.PathSeparator);

        var minSegmentCount = Math.Min(flowPacketPathSegments.Length,
            flowPathSegments.Length);

        for (var i = 0; i < minSegmentCount; i++)
        {
            var flowPacketSegment = flowPacketPathSegments[i];
            var flowSegment = flowPathSegments[i];

            if (flowSegment == IPathMatcher.WildCard || flowPacketSegment == IPathMatcher.WildCard)
                continue;

            if (IsRegex(flowSegment))
            {
                var regexPattern = ExtractRegex(flowSegment);
                if (!Regex.IsMatch(flowPacketSegment, regexPattern))
                    return false;
            } else if (IsRegex(flowPacketSegment))
            {
                var regexPattern = ExtractRegex(flowPacketSegment);
                if (!Regex.IsMatch(flowSegment, regexPattern))
                    return false;
            } else
            {
                if (!flowPacketSegment.Equals(flowSegment, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsRegex(string segment)
    {
        return segment.StartsWith("{") && segment.EndsWith("}");
    }

    private string ExtractRegex(string segment)
    {
        return segment.Substring(1, segment.Length - 2);
    }
}