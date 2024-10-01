using System.Net;
using FlowBroker.Client.ConnectionManagement;
using FlowBroker.Client.DataProcessing;
using FlowBroker.Client.Payload;
using FlowBroker.Client.Subscriptions;
using FlowBroker.Client.TaskManager;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Serialization;

namespace FlowBroker.Client.BrokerClient;

public interface IBrokerClient : IAsyncDisposable
{
    public bool Connected { get; }

    public IConnectionManager ConnectionManager { get; }

    void Connect(EndPoint endPoint);
    void Reconnect();
    void Disconnect();

    Task<ISubscription> GetFlowSubscriptionAsync(string name,
        CancellationToken? cancellationToken = null);

    Task<SendAsyncResult> PublishAsync(string flowPath, byte[] data,
        CancellationToken? cancellationToken = null);

    Task<SendAsyncResult> DeclareFlowAsync(string name, string flowPath,
        CancellationToken? cancellationToken = null);

    Task<SendAsyncResult> DeleteFlowAsync(string name,
        CancellationToken? cancellationToken = null);

    Task<SendAsyncResult> ConfigureClientAsync(int prefetchCount,
        CancellationToken? cancellationToken = null);
}

public class BrokerClient : IBrokerClient
{
    private readonly IPayloadFactory _payloadFactory;
    private readonly ISendDataProcessor _sendDataProcessor;
    private readonly ISerializer _serializer;
    private readonly ISubscriptionRepository _subscriptionStore;
    private readonly ITaskManager _taskManager;

    private bool _isDisposed;

    public BrokerClient(IPayloadFactory payloadFactory,
        IConnectionManager connectionManager,
        ISendDataProcessor sendDataProcessor,
        ISubscriptionRepository subscriptionStore, ISerializer serializer,
        ITaskManager taskManager)
    {
        _payloadFactory = payloadFactory;
        ConnectionManager = connectionManager;
        _subscriptionStore = subscriptionStore;
        _sendDataProcessor = sendDataProcessor;
        _serializer = serializer;
        _taskManager = taskManager;
    }

    public bool Connected => ConnectionManager.Socket.Connected;

    public IConnectionManager ConnectionManager { get; }

    public void Connect(EndPoint endPoint)
    {
        ConnectionManager.Connect(endPoint);
    }

    public void Reconnect()
    {
        ConnectionManager.Reconnect();
    }

    public void Disconnect()
    {
        ConnectionManager.Disconnect();
    }

    public async Task<ISubscription> GetFlowSubscriptionAsync(string name,
        CancellationToken? cancellationToken = null)
    {
        var subscription = new Subscription(_payloadFactory, ConnectionManager,
            _sendDataProcessor);

        _subscriptionStore.Add(name, subscription);

        await subscription.SetupAsync(name,
            cancellationToken ?? CancellationToken.None);

        return subscription;
    }

    public Task<SendAsyncResult> PublishAsync(string flowPath, byte[] data,
        CancellationToken? cancellationToken = null)
    {
        var serializedPayload =
            _payloadFactory.NewPacket(FlowPacketType.FlowPacket, flowPath,
                data);
        return _sendDataProcessor.SendAsync(serializedPayload, true,
            cancellationToken ?? CancellationToken.None);
    }

    public Task<SendAsyncResult> DeclareFlowAsync(string name, string flowPath,
        CancellationToken? cancellationToken = null)
    {
        var serializedPayload =
            _payloadFactory.NewPacketFlowName(FlowPacketType.FlowDeclare, flowPath, name);
        return _sendDataProcessor.SendAsync(serializedPayload, true,
            cancellationToken ?? CancellationToken.None);
    }

    public Task<SendAsyncResult> DeleteFlowAsync(string name,
        CancellationToken? cancellationToken = null)
    {
        var serializedPayload =
            _payloadFactory.NewPacketFlowName(FlowPacketType.FlowDelete, null, name);
        return _sendDataProcessor.SendAsync(serializedPayload, true,
            cancellationToken ?? CancellationToken.None);
    }

    public Task<SendAsyncResult> ConfigureClientAsync(int prefetchCount,
        CancellationToken? cancellationToken = null)
    {
        var serializedPayload =
            _payloadFactory.NewPacket(FlowPacketType.Configure, null);
        return _sendDataProcessor.SendAsync(serializedPayload, true,
            cancellationToken ?? CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(BrokerClient));

        _isDisposed = true;

        await _subscriptionStore.DisposeAsync();

        _taskManager.Dispose();
    }
}
