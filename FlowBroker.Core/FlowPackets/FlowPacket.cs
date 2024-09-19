using System.Buffers;
using FlowBroker.Core.Persistence;

namespace FlowBroker.Core.FlowPackets;

public enum FlowPacketType
{
    Message = 1,
    Ok = 2,
    Error = 3,
    Ack = 4,
    Nack = 5,
    TopicDeclare = 6,
    TopicDelete = 7,
    SubscribeTopic = 8,
    UnsubscribeTopic = 9,
    Ready = 10,
    Configure = 11,
    TopicMessage = 12
}

public class FlowPacket
{
    public Guid Id { get; set; }
    public string FlowName { get; set; }
    public string FlowPath { get; set; }
    public Memory<byte> Data { get; set; }
    public byte[] OriginalFlowPacketData { get; set; }

    public FlowPacketType PacketType { get; set; } = FlowPacketType.Message;

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
