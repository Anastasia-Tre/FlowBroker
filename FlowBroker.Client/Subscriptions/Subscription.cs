using FlowBroker.Client.ConnectionManagement;
using FlowBroker.Client.DataProcessing;
using FlowBroker.Client.Payload;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Utils.BinarySerialization;
using FlowBroker.Core.Utils.Persistence;

namespace FlowBroker.Client.Subscriptions;

public interface ISubscription : IAsyncDisposable
{
    string Name { get; }

    bool AddPacketHandler(Type type, Action<SubscriptionPacket> action);
    void OnPacketReceived(FlowPacket flowPacket);
}

internal class Subscription : ISubscription
{
    private readonly IConnectionManager _connectionManager;
    private readonly ISendDataProcessor _sendDataProcessor;
    private readonly ISerializedPayloadFactory _serializedPayloadFactory;
    private bool _disposed;

    //public event Action<SubscriptionPacket> PacketReceived;

    private readonly Dictionary<Type, Action<SubscriptionPacket>>
        PacketHandlers = new();

    public Subscription(ISerializedPayloadFactory serializedPayloadFactory,
        IConnectionManager connectionManager,
        ISendDataProcessor sendDataProcessor)
    {
        _serializedPayloadFactory = serializedPayloadFactory;
        _connectionManager = connectionManager;
        _sendDataProcessor = sendDataProcessor;

        connectionManager.OnConnected += OnConnected;
    }

    public string Name { get; private set; }

    public bool AddPacketHandler(Type type, Action<SubscriptionPacket> action)
    {
        return PacketHandlers.TryAdd(type, action);
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        //PacketReceived = null;
        PacketHandlers.Clear();

        _connectionManager.OnConnected -= OnConnected;

        var cancellationTokenSource =
            new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await UnSubscribeAsync(cancellationTokenSource.Token);
    }

    public void OnPacketReceived(FlowPacket flowPacket)
    {
        try
        {
            ThrowIfDisposed();

            var subscriptionPacket = new SubscriptionPacket
            {
                PacketId = flowPacket.Id,
                DataType = flowPacket.DataType,
                Data = JsonHelper.ByteArrayToObject(flowPacket.Data.ToArray(),
                    flowPacket.DataType),
                FlowFlowPath = flowPacket.FlowPath,
                FlowName = flowPacket.FlowName
            };

            subscriptionPacket.OnPacketProcessedByClient +=
                OnPacketProcessedByClient;

            //PacketReceived?.Invoke(subscriptionPacket);
            GetPacketHandler(flowPacket.DataType).Invoke(subscriptionPacket);
        }
        // if packet process failed then mark it as nacked
        catch
        {
            var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromMinutes(1));
            OnPacketProcessedByClient(flowPacket.Id, false,
                cancellationTokenSource.Token);
        }
    }

    private Action<SubscriptionPacket> GetPacketHandler(Type type)
    {
        if (PacketHandlers.TryGetValue(type, out var action))
            return action;
        return _ => { };
    }

    public async Task SetupAsync(string name,
        CancellationToken cancellationToken)
    {
        Name = name;
        await SubscribeAsync(cancellationToken);
    }

    private async void OnPacketProcessedByClient(Guid packetId, bool ack,
        CancellationToken cancellationToken)
    {
        if (ack)
            await AckAsync(packetId, cancellationToken);
        else
            await NackAsync(packetId, cancellationToken);
    }

    private async Task SubscribeAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var serializedPayload =
            _serializedPayloadFactory.NewPacketFlowName(
                FlowPacketType.SubscribeFlow,
                null, Name);

        var result = await _sendDataProcessor.SendAsync(serializedPayload, true,
            cancellationToken);

        if (!result.IsSuccess)
            throw new Exception(
                $"Failed to create subscription, error: {result.InternalErrorCode}");
    }

    private async Task UnSubscribeAsync(CancellationToken cancellationToken)
    {
        var serializedPayload =
            _serializedPayloadFactory.NewPacketFlowName(
                FlowPacketType.UnsubscribeFlow,
                null, Name);

        try
        {
            await _sendDataProcessor.SendAsync(serializedPayload, true,
                cancellationToken);
        }
        catch (ObjectDisposedException)
        {
            // ignore ObjectDisposedException
        }
    }

    private async Task AckAsync(Guid packetId,
        CancellationToken cancellationToken)
    {
        var serializedPayload = _serializedPayloadFactory.NewPacket(
            FlowPacketType.Ack,
            null, packetId);
        await _sendDataProcessor.SendAsync(serializedPayload, false,
            cancellationToken);
    }

    private async Task NackAsync(Guid packetId,
        CancellationToken cancellationToken)
    {
        var serializedPayload = _serializedPayloadFactory.NewPacket(
            FlowPacketType.Nack,
            null, packetId);
        await _sendDataProcessor.SendAsync(serializedPayload, false,
            cancellationToken);
    }


    private async void OnConnected(object connectionManager, EventArgs e)
    {
        await SubscribeAsync(CancellationToken.None);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(
                $"{nameof(Subscription)} is disposed and cannot be accessed");
    }
}

public interface
    ISubscriptionRepository : IDataRepository<string, ISubscription>,
    IAsyncDisposable
{
    bool TryGet<T>(string flowName, out ISubscription subscription)
        where T : class, IPacket;
}

public class SubscriptionRepository : DataRepository<string, ISubscription>,
    ISubscriptionRepository
{
    public bool TryGet<T>(string flowName, out ISubscription subscription)
        where T : class, IPacket
    {
        if (TryGet(flowName, out var subscriptionBase))
            if (subscriptionBase is Subscription typedSubscription)
            {
                subscription = typedSubscription;
                return true;
            }

        subscription = null;
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, subscriber) in Data) await subscriber.DisposeAsync();
    }
}
