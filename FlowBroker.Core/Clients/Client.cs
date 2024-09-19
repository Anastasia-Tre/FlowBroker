using FlowBroker.Core.Payload;
using FlowBroker.Core.Utils.Persistence;

namespace FlowBroker.Core.Clients;

public interface IClient : IDisposable
{
    Guid Id { get; }
    bool IsClosed { get; }

    event EventHandler<EventArgs> OnDisconnected;
    event EventHandler<EventArgs> OnDataReceived;

    void Setup();

    void StartReceiveProcess();
    void StartSendProcess();
    Task SendNextFlowPacketInQueue();

    Task<bool> SendAsync(Memory<byte> payload,
        CancellationToken cancellationToken);

    void EnqueueFireAndForget(SerializedPayload serializedPayload);

    void OnPayloadAckReceived(Guid payloadId);
    void OnPayloadNackReceived(Guid payloadId);

    void Close();
}

public class Client : IClient
{
    public Guid Id { get; }
    public bool IsClosed { get; }
    public event EventHandler<EventArgs>? OnDisconnected;
    public event EventHandler<EventArgs>? OnDataReceived;

    public void Setup()
    {
        throw new NotImplementedException();
    }

    public void StartReceiveProcess()
    {
        throw new NotImplementedException();
    }

    public void StartSendProcess()
    {
        throw new NotImplementedException();
    }

    public Task SendNextFlowPacketInQueue()
    {
        throw new NotImplementedException();
    }

    public Task<bool> SendAsync(Memory<byte> payload,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void EnqueueFireAndForget(SerializedPayload serializedPayload)
    {
        throw new NotImplementedException();
    }

    public void OnPayloadAckReceived(Guid payloadId)
    {
        throw new NotImplementedException();
    }

    public void OnPayloadNackReceived(Guid payloadId)
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public interface IClientRepository : IDataRepository<Guid, IClient>;

public class ClientRepository : DataRepository<Guid, IClient>, IClientRepository
{
}
