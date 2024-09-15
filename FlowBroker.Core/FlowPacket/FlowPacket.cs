using FlowBroker.Core.Persistence;
using System.Buffers;

namespace FlowBroker.Core.FlowPacket;

public struct FlowPacket
{
    public Guid Id { get; set; }
    public string FlowName { get; set; }
    public string FlowPath { get; set; }
    public Memory<byte> Data { get; set; }
    public byte[] OriginalFlowPacketData { get; set; }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(OriginalFlowPacketData);
    }
}

public interface IFlowPacketRepository : IDataRepository<Guid, FlowPacket>;

public class FlowPacketRepository : DataRepository<Guid, FlowPacket>, IFlowPacketRepository
{
}
