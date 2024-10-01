using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Utils.Pooling;

namespace FlowBroker.Core.Serialization;

public interface IDeserializer
{
    FlowPacketType ParseFlowPacketType(Memory<byte> b);

    FlowPacket Deserialized(FlowPacketType type, Memory<byte> data);
}

public class Deserializer : IDeserializer
{
    public FlowPacketType ParseFlowPacketType(Memory<byte> b)
    {
        var typeSlice =
            BitConverter.ToInt32(
                b.Span[..BinaryProtocolConfiguration.SizeForInt]);
        return (FlowPacketType)typeSlice;
    }

    public FlowPacket Deserialized(FlowPacketType type, Memory<byte> data)
    {
        var binaryReader = ObjectPool.Shared.Get<BinaryProtocolReader>();
        binaryReader.Setup(data);

        try
        {
            var packetId = binaryReader.ReadNextGuid();

            var isPath = binaryReader.ReadNextInt();
            var path = isPath == 1 ? binaryReader.ReadNextString() : null;

            var isQueueName = binaryReader.ReadNextInt();
            var queueName = isQueueName == 1 ? binaryReader.ReadNextString() : null;

            var dataSize = binaryReader.ReadNextBytes();

            return new FlowPacket
            {
                Id = packetId,
                PacketType = type,
                FlowPath = path,
                FlowName = queueName,
                Data = dataSize.OriginalData.AsMemory(0, dataSize.Size),
                OriginalFlowPacketData = dataSize.OriginalData
            };
        }
        finally
        {
            ObjectPool.Shared.Return(binaryReader);
        }
    }
}
