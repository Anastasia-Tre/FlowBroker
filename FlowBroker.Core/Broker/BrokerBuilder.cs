using FlowBroker.Core.Client;
using FlowBroker.Core.Flow;
using FlowBroker.Core.FlowPacket;
using Microsoft.Extensions.DependencyInjection;

namespace FlowBroker.Core.Broker;

public class BrokerBuilder
{
    private readonly IServiceCollection _serviceCollection;

    public BrokerBuilder()
    {
        _serviceCollection = new ServiceCollection();

        _serviceCollection.AddSingleton<IBroker, Broker>();

        _serviceCollection.AddSingleton<IClientRepository, ClientRepository>();
        _serviceCollection.AddSingleton<IFlowRepository, FlowRepository>();
        _serviceCollection.AddSingleton<IFlowPacketRepository, FlowPacketRepository>();
    }

    public IBroker Build()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IBroker>();
    }
}
