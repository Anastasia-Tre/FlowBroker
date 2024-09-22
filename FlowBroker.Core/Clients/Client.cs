using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Tcp;
using FlowBroker.Core.Utils.Persistence;
using FlowBroker.Core.Utils.Pooling;
using Microsoft.Extensions.Logging;

namespace FlowBroker.Core.Clients;

public interface IClient : IDisposable
{
    Guid Id { get; }
    bool IsClosed { get; }

    event EventHandler<EventArgs> OnDisconnected;
    event EventHandler<ClientSessionDataReceivedEventArgs> OnDataReceived;

    void Setup(ISocket socket);

    void StartReceiveProcess();
    void StartSendProcess();
    Task SendNextFlowPacketInQueue();

    Task<bool> SendAsync(Memory<byte> payload,
        CancellationToken cancellationToken);

    AsyncPayloadTicket Enqueue(SerializedPayload serializedPayload);
    void EnqueueFireAndForget(SerializedPayload serializedPayload);

    void OnPayloadAckReceived(Guid payloadId);
    void OnPayloadNackReceived(Guid payloadId);

    void Close();
}

public class Client : IClient
{
    public Guid Id { get; }
    public bool IsClosed { get; set; }
    public event EventHandler<EventArgs>? OnDisconnected;
    public event EventHandler<ClientSessionDataReceivedEventArgs>? OnDataReceived;

    private readonly IBinaryDataProcessor _binaryDataProcessor;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<Client> _logger;
    private readonly Channel<SerializedPayload> _queue;
    private readonly byte[] _receiveBuffer;
    private readonly ConcurrentDictionary<Guid, AsyncPayloadTicket> _tickets;
    private ISocket _socket;

    public Client(ILogger<Client> logger, IBinaryDataProcessor binaryDataProcessor = null)
    {
        _logger = logger;

        Id = Guid.NewGuid();

        _binaryDataProcessor = binaryDataProcessor ?? new BinaryDataProcessor();

        _receiveBuffer = ArrayPool<byte>.Shared.Rent(BinaryProtocolConfiguration.ReceiveDataSize);

        _tickets = new ConcurrentDictionary<Guid, AsyncPayloadTicket>();

        _cancellationTokenSource = new CancellationTokenSource();

        _queue = Channel.CreateUnbounded<SerializedPayload>();
    }

    public void Setup(ISocket socket)
    {
        if (!socket.Connected)
            throw new InvalidOperationException("The provided tcp socket was not in connected state");

        _socket = socket;
    }

    public void StartReceiveProcess()
    {
        ThrowIfDisposed();

        Task.Factory.StartNew(async () =>
        {
            while (!IsClosed) await ReceiveAsync();

            OnReceivedDataDisposed();
        }, TaskCreationOptions.LongRunning);
    }

    private void ProcessReceivedData()
    {
        try
        {
            _binaryDataProcessor.BeginLock();

            while (_binaryDataProcessor.TryRead(out var binaryPayload))
                try
                {
                    var dataReceivedEventArgs = new ClientSessionDataReceivedEventArgs
                    {
                        Data = binaryPayload.DataWithoutSize,
                        Id = Id
                    };

                    OnDataReceived?.Invoke(this, dataReceivedEventArgs);
                } finally
                {
                    binaryPayload.Dispose();
                }
        } finally
        {
            _binaryDataProcessor.EndLock();
        }
    }

    private async ValueTask ReceiveAsync()
    {
        var receivedSize = await _socket.ReceiveAsync(_receiveBuffer, _cancellationTokenSource.Token);

        if (receivedSize == 0)
        {
            Close();
            return;
        }

        _binaryDataProcessor.Write(_receiveBuffer.AsMemory(0, receivedSize));

        ProcessReceivedData();
    }

    public void StartSendProcess()
    {
        ThrowIfDisposed();

        Task.Factory.StartNew(async () =>
        {
            while (!IsClosed) await SendNextFlowPacketInQueue();
        });
    }

    public async Task SendNextFlowPacketInQueue()
    {
        var serializedPayload = await _queue.Reader.ReadAsync();

        var result = await SendAsync(serializedPayload.Data, CancellationToken.None);

        _logger.LogTrace($"Sending flowPacket: {serializedPayload.PayloadId} to client: {Id}");

        if (!result) DisposeFlowPacketPayloadAndSetStatus(serializedPayload.PayloadId, false);

        ObjectPool.Shared.Return(serializedPayload);
    }

    public async Task<bool> SendAsync(Memory<byte> payload, CancellationToken cancellationToken)
    {
        var sendSize = await _socket.SendAsync(payload, cancellationToken);

        if (sendSize == 0)
        {
            Close();
            return false;
        }

        return true;
    }

    public AsyncPayloadTicket Enqueue(SerializedPayload serializedPayload)
    {
        lock (this)
        {
            var queueWasSuccessful = _queue.Writer.TryWrite(serializedPayload);

            if (queueWasSuccessful)
            {
                _logger.LogTrace($"Enqueue message: {serializedPayload.PayloadId} in client: {Id}");

                var ticket = ObjectPool.Shared.Get<AsyncPayloadTicket>();

                ticket.Setup(serializedPayload.PayloadId);

                _tickets[ticket.PayloadId] = ticket;

                return ticket;
            }

            throw new ChannelClosedException();
        }
    }

    public void EnqueueFireAndForget(SerializedPayload serializedPayload)
    {
        _queue.Writer.TryWrite(serializedPayload);
    }

    public void OnPayloadAckReceived(Guid payloadId)
    {
        DisposeFlowPacketPayloadAndSetStatus(payloadId, true);
    }

    public void OnPayloadNackReceived(Guid payloadId)
    {
        DisposeFlowPacketPayloadAndSetStatus(payloadId, false);
    }

    public void Close()
    {
        lock (this)
        {
            if (IsClosed) return;

            try
            {
                _logger.LogInformation($"Dispose was called on client: {Id}");

                IsClosed = true;

                _queue.Writer.TryComplete();

                _cancellationTokenSource.Cancel();

                if (_socket.Connected) _socket.Disconnect();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    var disconnectedEventArgs = new ClientSessionDisconnectedEventArgs { Id = Id };
                    OnDisconnected?.Invoke(this, disconnectedEventArgs);
                    OnDisconnected = null;
                });
            } catch
            {
            }
        }
    }

    public void Dispose()
    {
        Close();
    }

    private void ThrowIfDisposed()
    {
        if (IsClosed)
            throw new ObjectDisposedException("Session has been disposed previously");
    }

    private void OnReceivedDataDisposed()
    {
        _binaryDataProcessor.Dispose();
        ArrayPool<byte>.Shared.Return(_receiveBuffer);
        OnDataReceived = null;
    }

    private void DisposeFlowPacketPayloadAndSetStatus(Guid payloadId, bool ack)
    {
        try
        {
            if (_tickets.Remove(payloadId, out var ticket))
            {
                var type = ack ? "Ack" : "nack";

                _logger.LogTrace($"{type} received for flowPacket: {payloadId}");

                ticket.SetStatus(ack);

                ObjectPool.Shared.Return(ticket);
            }
        } catch
        {
        }
    }
}

public interface IClientRepository : IDataRepository<Guid, IClient>;

public class ClientRepository : DataRepository<Guid, IClient>, IClientRepository
{
}

public sealed class ClientSessionDisconnectedEventArgs : System.EventArgs
{
    public Guid Id { get; set; }
}

public class ClientSessionDataReceivedEventArgs
{
    public Guid Id { get; set; }
    public Memory<byte> Data { get; set; }
}