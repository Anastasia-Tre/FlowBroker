using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Serialization;
using FlowBroker.Core.Utils.Persistence;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using FlowBroker.Core.RouteMatching;
using FlowBroker.Core.Utils.WaitThrottling;

namespace FlowBroker.Core.Flows;

public interface IFlow : IDisposable
{
    string Name { get; }
    string FlowPath { get; }

    void Setup(string name, string path);

    void StartProcessingFlowPackets();
    Task ReadNextFlowPacket();
    void OnFlowPacket(FlowPacket packet);

    void ClientSubscribed(IClient client);
    void ClientUnsubscribed(IClient client);

    bool FlowPacketPathMatch(string path);
}

public class Flow : IFlow
{
    private readonly IDispatcher _dispatcher;
    private readonly ILogger<Flow> _logger;

    private readonly IFlowPacketRepository _flowPacketRepo;

    private readonly Channel<Guid> _queueChannel;

    private readonly IRouteMatcher _pathMatcher;
    private readonly ISerializer _serializer;
    private readonly DynamicWaitThrottling _throttling;

    private bool _disposed;

    public Flow(IDispatcher dispatcher, IFlowPacketRepository flowPacketRepo, IRouteMatcher pathMatcher,
        ISerializer serializer, ILogger<Flow> logger)
    {
        _dispatcher = dispatcher;
        _flowPacketRepo = flowPacketRepo;
        _pathMatcher = pathMatcher;
        _serializer = serializer;
        _logger = logger;
        _queueChannel = Channel.CreateUnbounded<Guid>();
        _throttling = new DynamicWaitThrottling();
    }

    public string Name { get; private set; }

    public string FlowPath { get; private set; }


    public void Dispose()
    {
        _disposed = true;
        _ = _queueChannel.Writer.TryComplete();
    }

    public void Setup(string name, string path)
    {
        ThrowIfDisposed();

        Name = name;
        FlowPath = path;

        ReadPayloadsFromFlowPacketRepo();
    }

    public void StartProcessingFlowPackets()
    {
        Task.Factory.StartNew(async () =>
        {
            while (!_disposed) await ReadNextFlowPacket();
        }, TaskCreationOptions.LongRunning);
    }

    public void OnFlowPacket(FlowPacket flowPacket)
    {
        ThrowIfDisposed();

        _logger.LogInformation($"Flow {Name} received flowPacket with id: {flowPacket.Id}");

        // create FlowFlowPacket from flowPacket
        flowPacket.FlowName = Name;

        // persist the flowPacket
        _flowPacketRepo.Add(flowPacket.Id, flowPacket);

        // add the flowPacket to queue chan
        _queueChannel.Writer.TryWrite(flowPacket.Id);
    }

    public bool FlowPacketPathMatch(string flowPacketRoute)
    {
        if (_disposed) return false;

        return _pathMatcher.Match(flowPacketRoute, FlowPath);
    }

    public void ClientSubscribed(IClient client)
    {
        _logger.LogInformation($"Added new subscription to flow: {Name} with id: {client.Id}");
        ThrowIfDisposed();
        _dispatcher.Add(client);
    }

    public void ClientUnsubscribed(IClient client)
    {
        ThrowIfDisposed();
        var success = _dispatcher.Remove(client);
        if (success) _logger.LogInformation($"Removed subscription from flow: {Name} with id: {client.Id}");
    }

    public async Task ReadNextFlowPacket()
    {
        ThrowIfDisposed();

        if (_queueChannel.Reader.TryRead(out var flowPacketId)) await ProcessFlowPacket(flowPacketId);
    }

    private void ReadPayloadsFromFlowPacketRepo()
    {
        ThrowIfDisposed();

        var flowPackets = _flowPacketRepo.GetAll();

        if (flowPackets.Any())
            _logger.LogWarning($"Found {flowPackets.Count()} flowPackets while initializing the flow: {Name}");
        else
            _logger.LogWarning($"No flowPackets was found while initializing the flow: {Name}");

        foreach (var flowPacket in flowPackets)
            _queueChannel.Writer.TryWrite(flowPacket.Id);
    }

    private async Task ProcessFlowPacket(Guid flowPacketId)
    {
        if (_flowPacketRepo.TryGet(flowPacketId, out var flowPacket))
        {
            // convert the flowPacket to serialized payload
            var serializedPayload = _serializer.Serialize(flowPacket);

            await SendSerializedPayloadToNextAvailableClient(serializedPayload);
        }
    }

    private async ValueTask SendSerializedPayloadToNextAvailableClient(SerializedPayload serializedPayload)
    {
        // keep trying to find an available client 
        while (true)
        {
            var client = _dispatcher.NextAvailable();

            // if no subscription is found then just wait
            if (client is null)
            {
                await _throttling.WaitAndIncrease();
                continue;
            }

            // reset the _throttling to base value
            _throttling.Reset();

            // get ticket for payload
            try
            {
                _logger.LogInformation(
                    $"Adding flowPacket with id: {serializedPayload.PayloadId} to subscription with id: {client.Id} in flow: {Name}");
                var ticket = client.Enqueue(serializedPayload);
                ticket.OnStatusChanged += OnStatusChanged;
                break;
            } catch (ChannelClosedException)
            {
            }
        }
    }

    public void OnStatusChanged(Guid flowPacketId, bool ack)
    {
        if (ack)
            OnFlowPacketAck(flowPacketId);
        else
            OnFlowPacketNack(flowPacketId);
    }

    private void OnFlowPacketAck(Guid flowPacketId)
    {
        _logger.LogInformation($"Received ack for flowPacket with id: {flowPacketId} in flow: {Name}");
        _flowPacketRepo.Remove(flowPacketId);
    }

    private void OnFlowPacketNack(Guid flowPacketId)
    {
        _logger.LogInformation($"Received Nack for flowPacket with id: {flowPacketId} in flow: {Name}");
        _queueChannel.Writer.TryWrite(flowPacketId);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed))
            throw new ObjectDisposedException("Flow has been disposed");
    }
}