namespace FlowBroker.Main
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var workflow = new Workflow();
            await workflow.StartProcess();
        }
    }
}
