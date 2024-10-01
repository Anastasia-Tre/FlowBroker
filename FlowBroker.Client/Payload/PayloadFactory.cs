using System.Text;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Serialization;

namespace FlowBroker.Client.Payload;

public interface IPayloadFactory
{
    SerializedPayload NewPacket(FlowPacketType type, string path,
        byte[] data = null);

    //SerializedPayload NewPacket(FlowPacketType type, string path, string data);

    SerializedPayload NewPacketFlowName(FlowPacketType type, string path,
        string flowName, byte[] data = null);

    //SerializedPayload NewPacketFlowName(FlowPacketType type, string path,
    //    string flowName, string data);
}

public class PayloadFactory : IPayloadFactory
{
    private readonly ISerializer _serializer;

    public PayloadFactory(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public SerializedPayload NewPacket(FlowPacketType type, string path,
        byte[] data = null)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            Data = data,
            FlowPath = path
        };

        return _serializer.Serialize(payload);
    }

    //public SerializedPayload NewPacket(FlowPacketType type, string path,
    //    string data)
    //{
    //    return NewPacket(type, path, Encoding.ASCII.GetBytes(data));
    //}

    public SerializedPayload NewPacketFlowName(FlowPacketType type, string path, string flowName,
        byte[] data = null)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            Data = data,
            FlowPath = path,
            FlowName = flowName
        };

        return _serializer.Serialize(payload);
    }

    //public SerializedPayload NewPacketFlowName(FlowPacketType type, string path, string flowName,
    //    string data)
    //{
    //    return NewPacketFlowName(type, path, flowName, Encoding.ASCII.GetBytes(data));
    //}
}
