namespace FlowBroker.Client.Subscriptions;

public class SubscriptionPacket
{
    public Guid PacketId { get; set; }

    public string FlowFlowPath { get; set; }
    public string FlowName { get; set; }

    public Memory<byte> Data { get; set; }

    internal event Action<Guid, bool, CancellationToken>
        OnPacketProcessedByClient;

    public void Ack(CancellationToken? cancellationToken = null)
    {
        CancellationToken token;

        if (cancellationToken is null)
        {
            var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromMinutes(1));
            token = cancellationTokenSource.Token;
        }
        else
        {
            token = cancellationToken.Value;
        }

        OnPacketProcessedByClient?.Invoke(PacketId, true, token);
    }

    public void Nack(CancellationToken? cancellationToken = null)
    {
        CancellationToken token;

        if (cancellationToken is null)
        {
            var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromMinutes(1));
            token = cancellationTokenSource.Token;
        }
        else
        {
            token = cancellationToken.Value;
        }

        OnPacketProcessedByClient?.Invoke(PacketId, false, token);
    }
}
