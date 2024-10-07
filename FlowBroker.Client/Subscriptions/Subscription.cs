using FlowBroker.Client.ConnectionManagement;
using FlowBroker.Client.DataProcessing;
using FlowBroker.Client.Payload;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Utils.Persistence;

namespace FlowBroker.Client.Subscriptions;

public interface ISubscription : IAsyncDisposable
{
    string Name { get; }

    event Action<SubscriptionPacket> PacketReceived;
}

internal class Subscription : ISubscription
{
    private readonly IConnectionManager _connectionManager;
    private readonly IPayloadFactory _payloadFactory;
    private readonly ISendDataProcessor _sendDataProcessor;
    private bool _disposed;

    public Subscription(IPayloadFactory payloadFactory,
        IConnectionManager connectionManager,
        ISendDataProcessor sendDataProcessor)
    {
        _payloadFactory = payloadFactory;
        _connectionManager = connectionManager;
        _sendDataProcessor = sendDataProcessor;

        connectionManager.OnConnected += OnConnected;
    }

    public event Action<SubscriptionPacket> PacketReceived;

    public string Name { get; private set; }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        PacketReceived = null;

        _connectionManager.OnConnected -= OnConnected;

        var cancellationTokenSource =
            new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await UnSubscribeAsync(cancellationTokenSource.Token);
    }

    public async Task SetupAsync(string name,
        CancellationToken cancellationToken)
    {
        Name = name;
        await SubscribeAsync(cancellationToken);
    }

    public void OnPacketReceived(FlowPacket flowPacket)
    {
        try
        {
            ThrowIfDisposed();

            var subscriptionPacket = new SubscriptionPacket
            {
                PacketId = flowPacket.Id,
                Data = flowPacket.Data,
                FlowFlowPath = flowPacket.FlowPath,
                FlowName = flowPacket.FlowName
            };

            subscriptionPacket.OnPacketProcessedByClient +=
                OnPacketProcessedByClient;

            PacketReceived?.Invoke(subscriptionPacket);
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
            _payloadFactory.NewPacketFlowName(FlowPacketType.SubscribeFlow,
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
            _payloadFactory.NewPacketFlowName(FlowPacketType.UnsubscribeFlow,
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
        var serializedPayload = _payloadFactory.NewPacket(FlowPacketType.Ack,
            null, packetId.ToByteArray());
        await _sendDataProcessor.SendAsync(serializedPayload, false,
            cancellationToken);
    }

    private async Task NackAsync(Guid packetId,
        CancellationToken cancellationToken)
    {
        var serializedPayload = _payloadFactory.NewPacket(FlowPacketType.Nack,
            null, packetId.ToByteArray());
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
    IAsyncDisposable;

public class SubscriptionRepository : DataRepository<string, ISubscription>,
    ISubscriptionRepository
{
    public async ValueTask DisposeAsync()
    {
        foreach (var (_, subscriber) in Data) await subscriber.DisposeAsync();
    }
}
