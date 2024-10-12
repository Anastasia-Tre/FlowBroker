using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Utils.BinarySerialization;

namespace FlowBroker.Client.Payload;

public static class FlowPacketFactory
{
    public static FlowPacket NewPacket(string path, IPacket packet)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = FlowPacketType.FlowPacket,
            DataType = packet.GetType(),
            Data = JsonHelper.ObjectToByteArray(packet, packet.GetType()),
            FlowPath = path
        };

        return payload;
    }

    public static FlowPacket NewPacket(FlowPacketType type, string path,
        byte[] data = null)
    {
        var payload = new FlowPacket
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            //DataType = data.GetType(),
            Data = data,
            FlowPath = path
        };

        return payload;
    }
}
