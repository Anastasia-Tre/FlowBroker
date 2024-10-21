using FlowBroker.Core.PathMatching;

namespace FlowBroker.Main;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var workflow = new Workflow<DefaultPathMatcher>();
        await workflow.StartProcess();
    }
}
