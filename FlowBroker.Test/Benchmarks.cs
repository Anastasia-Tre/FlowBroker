using FlowBroker.Core.PathMatching;
using FlowBroker.Main;

namespace FlowBroker.Test;

public class Benchmarks
{
    [Theory]
    [InlineData(1000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task Benchmark_DefaultPathMatcher_Test(int messageCount)
    {
        var workflow = new Workflow<DefaultPathMatcher>(messageCount);
        await workflow.StartProcess();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task Benchmark_RegexPathMatcher_Test(int messageCount)
    {
        var workflow = new Workflow<RegexPathMatcher>(messageCount);
        await workflow.StartProcess();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task Benchmark_CachedPathMatcher_Test(int messageCount)
    {
        var workflow = new Workflow<CachedPathMatcher>(messageCount);
        await workflow.StartProcess();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task Benchmark_CachedRegexPathMatcher_Test(int messageCount)
    {
        var workflow = new Workflow<CachedRegexPathMatcher>(messageCount);
        await workflow.StartProcess();
    }
}
