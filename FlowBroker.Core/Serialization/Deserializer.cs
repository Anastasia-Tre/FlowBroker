using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Utils.Pooling;

namespace FlowBroker.Core.Serialization;

public interface IDeserializer
{
    FlowPacketType ParseFlowPacketType(Memory<byte> b);

    FlowPacket Deserialized(Memory<byte> data);
}

public class Deserializer : IDeserializer
{
    public FlowPacketType ParseFlowPacketType(Memory<byte> b)
    {
        var typeSlice = BitConverter.ToInt32(b.Span[..BinaryProtocolConfiguration.SizeForInt]);
        return (FlowPacketType)typeSlice;
    }

    public FlowPacket Deserialized(Memory<byte> data)
    {
        var binaryReader = ObjectPool.Shared.Get<BinaryProtocolReader>();
        binaryReader.Setup(data);

        try
        {
            var packetId = binaryReader.ReadNextGuid();
            var route = binaryReader.ReadNextString();
            var queueName = binaryReader.ReadNextString();
            var dataSize = binaryReader.ReadNextBytes();

            return new FlowPacket
            {
                Id = packetId,
                FlowPath = route,
                FlowName = queueName,
                Data = dataSize.OriginalData.AsMemory(0, dataSize.Size),
                OriginalFlowPacketData = dataSize.OriginalData
            };
        } finally
        {
            ObjectPool.Shared.Return(binaryReader);
        }
    }
}
