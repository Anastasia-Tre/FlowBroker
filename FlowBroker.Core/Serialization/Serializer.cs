using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            return binaryWriter
                .WriteType(packet.PacketType)
                .WriteId(packet.Id)
                .WriteStr(packet.FlowPath)
                .WriteStr(packet.FlowName)
                .WriteMemory(packet.Data)
                .ToSerializedPayload();
        } finally
        {
            ObjectPool.Shared.Return(binaryWriter);
        }
    }
}
