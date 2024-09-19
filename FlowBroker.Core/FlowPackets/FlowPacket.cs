using System.Buffers;
using FlowBroker.Core.Utils.Persistence;

namespace FlowBroker.Core.FlowPackets;

public enum FlowPacketType
{
    FlowPacket = 1,
    Ok = 2,
    Error = 3,
    Ack = 4,
    Nack = 5,
    FlowDeclare = 6,
    FlowDelete = 7,
    SubscribeFlow = 8,
    UnsubscribeFlow = 9,
    Ready = 10,
    Configure = 11,
    FlowFlowPacket = 12
}

public class FlowPacket
{
    public Guid Id { get; set; }
    public string FlowName { get; set; }
    public string FlowPath { get; set; }
    public Memory<byte> Data { get; set; }
    public byte[] OriginalFlowPacketData { get; set; }

    public FlowPacketType PacketType { get; set; } = FlowPacketType.FlowPacket;

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(OriginalFlowPacketData);
    }
}

public interface IFlowPacketRepository : IDataRepository<Guid, FlowPacket>;

public class FlowPacketRepository : DataRepository<Guid, FlowPacket>,
    IFlowPacketRepository
{
}
