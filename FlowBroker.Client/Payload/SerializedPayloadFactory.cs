using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Serialization;
using FlowBroker.Core.Utils.BinarySerialization;

namespace FlowBroker.Client.Payload;

public interface ISerializedPayloadFactory
{
    //SerializedPayload NewPacket(FlowPacketType type, string path,
    //byte[] data = null);

    SerializedPayload NewPacket(string path, IPacket packet);

    SerializedPayload NewPacket(FlowPacketType type, string path,
        Guid data);

    SerializedPayload NewPacketFlowName(FlowPacketType type, string path,
        string flowName, byte[] data = null);
}

public class SerializedPayloadFactory : ISerializedPayloadFactory
{
    private readonly ISerializer _serializer;

    public SerializedPayloadFactory(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public SerializedPayload NewPacket(string path, IPacket packet)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = FlowPacketType.FlowPacket,
            DataType = packet.GetType(),
            Data = JsonHelper.ObjectToByteArray(packet, packet.GetType()),
            FlowPath = path
        };

        return _serializer.Serialize(payload);
    }

    public SerializedPayload NewPacket(FlowPacketType type, string path,
        Guid data)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            DataType = typeof(Guid),
            Data = data.ToByteArray(),
            FlowPath = path
        };

        return _serializer.Serialize(payload);
    }

    public SerializedPayload NewPacketFlowName(FlowPacketType type, string path,
        string flowName,
        byte[] data = null)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            //DataType = data?.GetType(),
            Data = data,
            FlowPath = path,
            FlowName = flowName
        };

        return _serializer.Serialize(payload);
    }

    public SerializedPayload NewPacket(FlowPacketType type, string path,
        byte[] data = null)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            //DataType = data?.GetType(),
            Data = data,
            FlowPath = path
        };

        return _serializer.Serialize(payload);
    }
}
