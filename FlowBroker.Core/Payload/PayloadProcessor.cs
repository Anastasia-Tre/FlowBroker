using System.Text;
using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Flows;
using FlowBroker.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace FlowBroker.Core.Payload;

public interface IPayloadProcessor
{
    void OnDataReceived(Guid sessionId, Memory<byte> data);
}

internal class PayloadProcessor : IPayloadProcessor
{
    private readonly IClientRepository _clientRepository;
    private readonly IDeserializer _deserializer;
    private readonly IFlowRepository _flowRepository;
    private readonly ILogger<PayloadProcessor> _logger;
    private readonly ISerializer _serializer;

    public PayloadProcessor(IDeserializer deserializer, ISerializer serializer,
        IClientRepository clientRepository,
        IFlowRepository flowRepository, ILogger<PayloadProcessor> logger)
    {
        _deserializer = deserializer;
        _serializer = serializer;
        _clientRepository = clientRepository;
        _flowRepository = flowRepository;
        _logger = logger;
    }

    public void OnDataReceived(Guid clientId, Memory<byte> data)
    {
        try
        {
            var type = _deserializer.ParseFlowPacketType(data);
            var packet = _deserializer.Deserialized(type, data);

            _logger.LogInformation(
                $"Received data with type: {type} from client: {clientId} : {Encoding.UTF8.GetString(packet.Data.ToArray())}");

            switch (type)
            {
                case FlowPacketType.FlowPacket:
                    OnFlowPacket(clientId, packet);
                    break;
                case FlowPacketType.Ack:
                    OnFlowPacketAck(clientId, packet);
                    break;
                case FlowPacketType.Nack:
                    OnFlowPacketNack(clientId, packet);
                    break;
                case FlowPacketType.SubscribeFlow:
                    OnSubscribeFlow(clientId, packet);
                    break;
                case FlowPacketType.UnsubscribeFlow:
                    OnUnsubscribeFlow(clientId, packet);
                    break;
                case FlowPacketType.FlowDeclare:
                    OnDeclareQueue(clientId, packet);
                    break;
                case FlowPacketType.FlowDelete:
                    OnDeleteQueue(clientId, packet);
                    break;
                case FlowPacketType.Configure:
                    OnConfigureClient(clientId, packet);
                    break;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Failed to deserialize data from client: {clientId} with error: {e}");
        }
    }


    private void OnFlowPacket(Guid clientId, FlowPacket packet)
    {
        var matchedAnyFlow = false;

        // dispatch the packet to matched queues
        foreach (var flow in _flowRepository.GetAll())
            if (flow.FlowPacketPathMatch(packet.FlowPath))
                try
                {
                    flow.OnFlowPacket(packet);
                    matchedAnyFlow = true;
                }
                // packet was not written, probably the channel was completed due to being disposed
                catch
                {
                    _logger.LogError(
                        $"Failed to write packet: {packet.Id} to flow: {flow.Name}");
                }

        if (matchedAnyFlow)
            // send received ack to publisher
            SendReceivedPayloadOk(clientId, packet.Id);
        else
            SendReceivePayloadError(clientId, packet.Id, "No flow was found");

        // must return the original packet data to buffer pool
        packet.Dispose();
    }

    private void OnFlowPacketAck(Guid clientId, FlowPacket ack)
    {
        _logger.LogInformation(
            $"Ack received for packet with id: {ack.Id} from client: {clientId}");

        if (_clientRepository.TryGet(clientId, out var client))
            client.OnPayloadAckReceived(ack.Id);
    }

    private void OnFlowPacketNack(Guid clientId, FlowPacket nack)
    {
        _logger.LogInformation(
            $"Nack received for packet with id: {nack.Id} from client: {clientId}");

        if (_clientRepository.TryGet(clientId, out var client))
            client.OnPayloadNackReceived(nack.Id);
    }

    private void OnSubscribeFlow(Guid clientId, FlowPacket subscribeFlow)
    {
        _clientRepository.TryGet(clientId, out var client);

        if (client is null)
        {
            _logger.LogWarning($"The client for id {clientId} was not found");
            SendReceivePayloadError(clientId, subscribeFlow.Id,
                "Internal error");
            return;
        }

        if (_flowRepository.TryGet(subscribeFlow.FlowName, out var flow))
        {
            flow.ClientSubscribed(client);
            SendReceivedPayloadOk(clientId, subscribeFlow.Id);
        }
        else
        {
            SendReceivePayloadError(clientId, subscribeFlow.Id,
                "Queue not found");
        }
    }

    private void OnUnsubscribeFlow(Guid clientId, FlowPacket unsubscribeFlow)
    {
        _clientRepository.TryGet(clientId, out var client);

        if (client is null)
        {
            _logger.LogWarning($"The client for id {clientId} was not found");
            SendReceivePayloadError(clientId, unsubscribeFlow.Id,
                "Internal error");
            return;
        }

        if (_flowRepository.TryGet(unsubscribeFlow.FlowName, out var queue))
        {
            queue.ClientUnsubscribed(client);
            SendReceivedPayloadOk(clientId, unsubscribeFlow.Id);
        }
        else
        {
            SendReceivePayloadError(clientId, unsubscribeFlow.Id,
                "Queue not found");
        }
    }


    private void OnDeclareQueue(Guid clientId, FlowPacket flowDeclare)
    {
        _logger.LogInformation($"declaring flow: {flowDeclare.FlowName}");

        // if queue exists
        if (_flowRepository.TryGet(flowDeclare.FlowName, out var queue))
        {
            // if queue path match
            if (queue.FlowPath == flowDeclare.FlowPath)
                SendReceivedPayloadOk(clientId, flowDeclare.Id);
            else
                SendReceivePayloadError(clientId, flowDeclare.Id,
                    "Queue name already exists");

            return;
        }

        // create new queue
        _flowRepository.Add(flowDeclare.FlowName, flowDeclare.FlowPath);

        SendReceivedPayloadOk(clientId, flowDeclare.Id);
    }

    private void OnDeleteQueue(Guid clientId, FlowPacket flowDelete)
    {
        _logger.LogInformation($"Deleting flow: {flowDelete.FlowName}");

        _flowRepository.Remove(flowDelete.FlowName);

        SendReceivedPayloadOk(clientId, flowDelete.Id);
    }

    private void OnConfigureClient(Guid clientId, FlowPacket configureClient)
    {
        if (_clientRepository.TryGet(clientId, out var client))
            SendReceivedPayloadOk(clientId, configureClient.Id);
        else
            SendReceivePayloadError(clientId, configureClient.Id,
                "Client not found");
    }

    private void SendReceivedPayloadOk(Guid clientId, Guid payloadId)
    {
        if (_clientRepository.TryGet(clientId, out var sendQueue))
        {
            var ok = new FlowPacket
            {
                Id = payloadId,
                PacketType = FlowPacketType.Ok
            };
            var sendPayload = _serializer.Serialize(ok);
            sendQueue.EnqueueFireAndForget(sendPayload);
        }
    }

    private void SendReceivePayloadError(Guid clientId, Guid payloadId,
        string packet)
    {
        if (_clientRepository.TryGet(clientId, out var sendQueue))
        {
            var error = new FlowPacket
            {
                Id = payloadId,
                Data = Encoding.ASCII.GetBytes(packet),
                PacketType = FlowPacketType.Error
            };
            var sendPayload = _serializer.Serialize(error);
            sendQueue.EnqueueFireAndForget(sendPayload);
        }
    }
}
