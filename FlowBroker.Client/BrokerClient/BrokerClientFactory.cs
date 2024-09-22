using FlowBroker.Client.ConnectionManagement;
using FlowBroker.Client.DataProcessing;
using FlowBroker.Client.Payload;
using FlowBroker.Client.Subscriptions;
using FlowBroker.Client.TaskManager;
using FlowBroker.Core.Clients;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Serialization;
using FlowBroker.Core.Utils.Pooling;
using Microsoft.Extensions.DependencyInjection;

namespace FlowBroker.Client.BrokerClient;

/// <summary>
/// Factory for creating <see cref="IBrokerClient" />
/// </summary>
public class BrokerClientFactory
{
    /// <summary>
    /// Instantiates and returns a new <see cref="IBrokerClient" />
    /// </summary>
    /// <returns>Created <see cref="IBrokerClient" /></returns>
    public IBrokerClient GetClient(Action<ServiceCollection> configure = default)
    {
        var serviceProvider = SetupServiceProvider(configure);

        return serviceProvider.GetRequiredService<IBrokerClient>();
    }

    private IServiceProvider SetupServiceProvider(Action<ServiceCollection> configure = default)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging();

        serviceCollection.AddSingleton<ISerializer, Serializer>();
        serviceCollection.AddSingleton<IDeserializer, Deserializer>();
        serviceCollection.AddSingleton<ITaskManager, TaskManager.TaskManager>();
        serviceCollection.AddSingleton<IPayloadFactory, PayloadFactory>();
        serviceCollection.AddSingleton<StringPool>();
        serviceCollection.AddSingleton<IConnectionManager, ConnectionManager>();
        serviceCollection.AddSingleton<ISendDataProcessor, SendDataProcessor>();
        serviceCollection.AddSingleton<IReceiveDataProcessor, ReceiveDataProcessor>();
        serviceCollection.AddSingleton<ISubscriptionRepository, SubscriptionRepository>();
        serviceCollection.AddSingleton<IBrokerClient, BrokerClient>();

        serviceCollection.AddTransient<IBinaryDataProcessor, BinaryDataProcessor>();
        serviceCollection.AddTransient<ISubscription, Subscription>();
        serviceCollection.AddTransient<IClient, Core.Clients.Client>();

        configure?.Invoke(serviceCollection);

        return serviceCollection.BuildServiceProvider();
    }
}