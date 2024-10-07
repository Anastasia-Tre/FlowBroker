using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Utils.Pooling;

namespace FlowBroker.Core.Serialization;

public interface ISerializer
{
    SerializedPayload Serialize(FlowPacket packet);
}

public class Serializer : ISerializer
{
    public SerializedPayload Serialize(FlowPacket packet)
    {
        var binaryWriter = ObjectPool.Shared.Get<BinaryProtocolWriter>();

        try
        {
            var result = binaryWriter
                    .WriteType(packet.PacketType)
                    .WriteId(packet.Id)
                //.WriteStr(packet.FlowPath)
                //.WriteStr(packet.FlowName ?? packet.FlowPath)
                //.WriteMemory(packet.Data)
                ;

            if (packet.FlowPath != null)
            {
                result.WriteInt(1);
                result.WriteStr(packet.FlowPath);
            }
            else
            {
                result.WriteInt(0);
            }

            if (packet.FlowName != null || packet.FlowPath != null)
            {
                result.WriteInt(1);
                result.WriteStr(packet.FlowName ?? packet.FlowPath);
            }
            else
            {
                result.WriteInt(0);
            }

            return result
                .WriteMemory(packet.Data)
                .ToSerializedPayload();
        }
        finally
        {
            ObjectPool.Shared.Return(binaryWriter);
        }
    }
}
