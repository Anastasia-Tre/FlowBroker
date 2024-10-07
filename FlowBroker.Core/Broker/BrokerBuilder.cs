using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Flows;
using FlowBroker.Core.Payload;
using FlowBroker.Core.PathMatching;
using FlowBroker.Core.Serialization;
using FlowBroker.Core.Tcp;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Microsoft.Extensions.Logging;

namespace FlowBroker.Core.Broker;

public class BrokerBuilder
{
    private readonly IServiceCollection _serviceCollection;

    public BrokerBuilder()
    {
        _serviceCollection = new ServiceCollection();

        _serviceCollection.AddLogging();

        _serviceCollection.AddSingleton<IBroker, Broker>();

        _serviceCollection.AddTransient<IClient, Client>();
        _serviceCollection.AddTransient<IFlow, Flow>();

        _serviceCollection.AddSingleton<IClientRepository, ClientRepository>();
        _serviceCollection.AddSingleton<IFlowRepository, FlowRepository>();
        _serviceCollection
            .AddSingleton<IFlowPacketRepository, FlowPacketRepository>();

        _serviceCollection.AddSingleton<IListener, TcpListener>();

        _serviceCollection.AddSingleton<ISerializer, Serializer>();
        _serviceCollection.AddSingleton<IDeserializer, Deserializer>();

        _serviceCollection.AddSingleton<IPayloadProcessor, PayloadProcessor>();
        _serviceCollection.AddSingleton<IPathMatcher, DefaultPathMatcher>();
        _serviceCollection.AddTransient<IDispatcher, Dispatcher>();
    }

    public IBroker Build()
    {
        var serviceProvider = _serviceCollection.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IBroker>();
    }

    public BrokerBuilder UseEndPoint(IPEndPoint endPoint)
    {
        var connectionProvider = new ConnectionProvider { IpEndPoint = endPoint };
        _serviceCollection.AddSingleton(connectionProvider);
        return this;
    }

    public BrokerBuilder ConfigureLogger(Action<ILoggingBuilder> loggerBuilder)
    {
        _serviceCollection.AddLogging(loggerBuilder);
        return this;
    }

    public BrokerBuilder AddConsoleLog()
    {
        ConfigureLogger(builder => { builder.AddConsole(); });
        return this;
    }

    public BrokerBuilder UsePathMatcher<T>() where T : class, IPathMatcher
    {
        _serviceCollection.AddSingleton<IPathMatcher, T>();
        return this;
    }
}
