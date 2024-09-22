using System.Runtime.InteropServices.JavaScript;
using FlowBroker.Client.Subscriptions;
using FlowBroker.Client.TaskManager;
using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Serialization;

namespace FlowBroker.Client.DataProcessing;

public interface IReceiveDataProcessor
{
    void DataReceived(object clientSessionObject, ClientSessionDataReceivedEventArgs dataReceivedEventArgs);

    event Action<Guid> OnOkReceived;
    event Action<Guid, FlowPacket> OnErrorReceived;
}

public class ReceiveDataProcessor : IReceiveDataProcessor
{
    private readonly IDeserializer _deserializer;
    private readonly ISubscriptionRepository _subscriptionStore;
    private readonly ITaskManager _taskManager;

    private int _receivedPacketsCount;

    public ReceiveDataProcessor(IDeserializer deserializer,
        ISubscriptionRepository subscriptionStore, ITaskManager taskManager)
    {
        _deserializer = deserializer;
        _subscriptionStore = subscriptionStore;
        _taskManager = taskManager;
    }

    public event Action<Guid> OnOkReceived;

    public event Action<Guid, FlowPacket> OnErrorReceived;

    public void DataReceived(object clientSessionObject, ClientSessionDataReceivedEventArgs dataReceivedEventArgs)
    {
        var data = dataReceivedEventArgs.Data;
        var payloadType = _deserializer.ParseFlowPacketType(data);
        switch (payloadType)
        {
            case FlowPacketType.Ok:
                OnOk(data);
                break;
            case FlowPacketType.Error:
                OnError(data);
                break;
            case FlowPacketType.FlowFlowPacket:
                OnPacket(data);
                break;
            default:
                throw new InvalidOperationException(
                    "Failed to map type to appropriate action while parsing payload");
        }
    }


    private void OnPacket(Memory<byte> payloadData)
    {
        Interlocked.Increment(ref _receivedPacketsCount);
        var queuePacket = _deserializer.Deserialized(payloadData);
        if (_subscriptionStore.TryGet(queuePacket.FlowName, out var subscription))
            ((Subscription) subscription).OnPacketReceived(queuePacket);
    }

    private void OnOk(Memory<byte> payloadData)
    {
        var ack = _deserializer.Deserialized(payloadData);
        _taskManager.OnPayloadOkResult(ack.Id);
        OnOkReceived?.Invoke(ack.Id);
    }

    private void OnError(Memory<byte> payloadData)
    {
        var nack = _deserializer.Deserialized(payloadData);
        _taskManager.OnPayloadErrorResult(nack.Id, nack);
        OnErrorReceived?.Invoke(nack.Id, nack);
    }
}