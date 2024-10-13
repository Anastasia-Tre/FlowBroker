using System.Collections.Concurrent;
using FlowBroker.Core.Utils.Persistence;

namespace FlowBroker.Core.FlowPackets;

public interface IFlowPacketRepository : IDataRepository<Guid, FlowPacket>;

public class FlowPacketRepository : DataRepository<Guid, FlowPacket>,
    IFlowPacketRepository
{
    private readonly
        ConcurrentDictionary<FlowPacketPriority,
            ConcurrentDictionary<Guid, FlowPacket>> _priorityQueues;

    public FlowPacketRepository()
    {
        _priorityQueues =
            new ConcurrentDictionary<FlowPacketPriority,
                ConcurrentDictionary<Guid, FlowPacket>>();

        foreach (FlowPacketPriority priority in
                 Enum.GetValues(typeof(FlowPacketPriority)))
            _priorityQueues[priority] =
                new ConcurrentDictionary<Guid, FlowPacket>();
    }

    public override void Add(Guid key, FlowPacket value)
    {
        if (!_priorityQueues[value.Priority].ContainsKey(key))
            _priorityQueues[value.Priority].TryAdd(key, value);
        else
            throw new ArgumentException(
                $"An item with the key {key} already exists.");
    }

    public override bool TryGet(Guid key, out FlowPacket value)
    {
        foreach (var priorityQueue in _priorityQueues.Values)
            if (priorityQueue.TryGetValue(key, out value))
                return true;
        value = null;
        return false;
    }

    public override bool Remove(Guid key)
    {
        foreach (var priorityQueue in _priorityQueues.Values)
            if (priorityQueue.TryRemove(key, out _))
                return true;
        return false;
    }

    public override IEnumerable<FlowPacket> GetAll()
    {
        foreach (var priority in Enum.GetValues(typeof(FlowPacketPriority))
                     .Cast<FlowPacketPriority>().OrderBy(p => p))
        foreach (var flowPacket in _priorityQueues[priority].Values)
            yield return flowPacket;
    }

    public FlowPacket? GetNextPacket()
    {
        foreach (var priority in Enum.GetValues(typeof(FlowPacketPriority))
                     .Cast<FlowPacketPriority>().OrderBy(p => p))
        {
            var priorityQueue = _priorityQueues[priority];
            if (priorityQueue.Any())
            {
                var firstPacket = priorityQueue.First();
                _priorityQueues[priority]
                    .TryRemove(firstPacket.Key, out var packet);
                return packet;
            }
        }

        return null;
    }
}
