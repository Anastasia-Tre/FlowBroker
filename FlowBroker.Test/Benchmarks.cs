using FlowBroker.Main;

namespace FlowBroker.Test;

public class Benchmarks
{
    [Theory]
    [InlineData(1000)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task BenchmarkTest(int messageCount)
    {
        var workflow = new Workflow(messageCount);
        await workflow.StartProcess();
    }
}
