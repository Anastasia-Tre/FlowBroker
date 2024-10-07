using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowBroker.Core.PathMatching;

namespace FlowBroker.Test.PathMatching;

public abstract class PathMatcherBaseTests
{
    protected IPathMatcher _routeMatcher;

    [Theory]
    [InlineData("bar/foo", "bar/foo", true)] // Повний збіг
    [InlineData("foo/bar", "bar/foo", false)] // Відрізняються
    [InlineData("bar/*", "bar/foo", true)] // Wildcard в кінці
    [InlineData("*", "bar/foo", true)] // Повний wildcard
    [InlineData("bar/*/foo", "bar/foo", true)] // Wildcard у середині
    [InlineData("", "bar/foo", false)] // Порожній messagePath
    [InlineData("bar/foo", "", false)] // Порожній queuePath
    [InlineData("foo/bar/baz/qux", "foo/bar/baz/qux", true)] // Довгий шлях, збіг
    [InlineData("foo/bar/baz", "foo/bar/baz/qux", true)] // Довгий шлях, збіг
    [InlineData("foo/bar-baz_qux", "foo/bar-baz_qux", true)] // Шлях зі спецсимволами
    public void DefaultMatch_WithVariousPaths(string messagePath, string queuePath, bool expectedMatch)
    {
        var match = _routeMatcher.Match(messagePath, queuePath);

        Assert.Equal(expectedMatch, match);
    }
}

public class RegexPathMatcherTests : PathMatcherBaseTests
{
    public RegexPathMatcherTests() 
    {
        _routeMatcher = new RegexPathMatcher();
    }

    [Theory]
    [InlineData("bar/{[a-z]+}", "bar/foo", true)] // Регулярний вираз для сегмента
    [InlineData("bar/{[0-9]+}/foo", "bar/123/foo", true)] // Регулярний вираз для числа
    [InlineData("bar/{[0-9]+}", "bar/foo", false)] // Регулярний вираз не збігається
    [InlineData("*/foo/{[0-9]+}", "bar/foo/123", true)] // Поєднання wildcard і regex
    [InlineData("bar/*/foo/{[a-z]+}", "bar/baz/foo/abc", true)] // Wildcard + regex
    public void RegexMatch_WithVariousPaths(string messagePath, string queuePath, bool expectedMatch)
    {
        var match = _routeMatcher.Match(messagePath, queuePath);

        Assert.Equal(expectedMatch, match);
    }
}

public class DefaultPathMatcherTests : PathMatcherBaseTests
{
    public DefaultPathMatcherTests()
    {
        _routeMatcher = new DefaultPathMatcher();
    }
}

public class CachedPathMatcherTests : PathMatcherBaseTests
{
    public CachedPathMatcherTests()
    {
        _routeMatcher = new CachedPathMatcher();
    }

    [Theory]
    [InlineData("bar/foo", "bar/foo", true)] // Повний збіг
    [InlineData("foo/bar", "bar/foo", false)] // Відрізняються
    [InlineData("bar/*", "bar/foo", true)] // Wildcard в кінці
    [InlineData("*", "bar/foo", true)] // Повний wildcard
    [InlineData("bar/*/foo", "bar/foo", true)] // Wildcard у середині
    [InlineData("", "bar/foo", false)] // Порожній messagePath
    [InlineData("bar/foo", "", false)] // Порожній queuePath
    [InlineData("foo/bar/baz/qux", "foo/bar/baz/qux", true)] // Довгий шлях, збіг
    [InlineData("foo/bar/baz", "foo/bar/baz/qux", true)] // Довгий шлях, збіг
    [InlineData("foo/bar-baz_qux", "foo/bar-baz_qux", true)] // Шлях зі спецсимволами
    public void CachedMatch_WithVariousPaths(string messagePath, string queuePath, bool expectedMatch)
    {
        var match = _routeMatcher.Match(messagePath, queuePath);

        Assert.Equal(expectedMatch, match);
    }
}

public class CachedRegexPathMatcherTests : PathMatcherBaseTests
{
    public CachedRegexPathMatcherTests()
    {
        _routeMatcher = new CachedRegexPathMatcher();
    }

    [Theory]
    [InlineData("bar/{[a-z]+}", "bar/foo", true)] // Регулярний вираз для сегмента
    [InlineData("bar/{[0-9]+}/foo", "bar/123/foo", true)] // Регулярний вираз для числа
    [InlineData("bar/{[0-9]+}", "bar/foo", false)] // Регулярний вираз не збігається
    [InlineData("*/foo/{[0-9]+}", "bar/foo/123", true)] // Поєднання wildcard і regex
    [InlineData("bar/*/foo/{[a-z]+}", "bar/baz/foo/abc", true)] // Wildcard + regex
    public void RegexMatch_WithVariousPaths(string messagePath, string queuePath, bool expectedMatch)
    {
        var match = _routeMatcher.Match(messagePath, queuePath);

        Assert.Equal(expectedMatch, match);
    }

    [Theory]
    [InlineData("bar/foo", "bar/foo", true)] // Повний збіг
    [InlineData("foo/bar", "bar/foo", false)] // Відрізняються
    [InlineData("bar/*", "bar/foo", true)] // Wildcard в кінці
    [InlineData("*", "bar/foo", true)] // Повний wildcard
    [InlineData("bar/*/foo", "bar/foo", true)] // Wildcard у середині
    [InlineData("", "bar/foo", false)] // Порожній messagePath
    [InlineData("bar/foo", "", false)] // Порожній queuePath
    [InlineData("foo/bar/baz/qux", "foo/bar/baz/qux", true)] // Довгий шлях, збіг
    [InlineData("foo/bar/baz", "foo/bar/baz/qux", true)] // Довгий шлях, збіг
    [InlineData("foo/bar-baz_qux", "foo/bar-baz_qux", true)] // Шлях зі спецсимволами
    public void CachedMatch_WithVariousPaths(string messagePath, string queuePath, bool expectedMatch)
    {
        var match = _routeMatcher.Match(messagePath, queuePath);

        Assert.Equal(expectedMatch, match);
    }
}