using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Persistence;

namespace FlowBroker.Core.Flows;

public interface IFlow : IDisposable
{
    string Name { get; }
    string FlowPath { get; }

    void Setup(string name, string route);

    void StartProcessingFlowPackets();
    Task ReadNextFlowPacket();
    void OnFlowPacket(FlowPacket packet);

    void ClientSubscribed(IClient client);
    void ClientUnsubscribed(IClient client);

    bool FlowPacketPathMatch(string path);
}

public class Flow : IFlow
{
    public string Name { get; }
    public string FlowPath { get; }
    public void Setup(string name, string route)
    {
        throw new NotImplementedException();
    }

    public void StartProcessingFlowPackets()
    {
        throw new NotImplementedException();
    }

    public Task ReadNextFlowPacket()
    {
        throw new NotImplementedException();
    }

    public void OnFlowPacket(FlowPacket packet)
    {
        throw new NotImplementedException();
    }

    public void ClientSubscribed(IClient client)
    {
        throw new NotImplementedException();
    }

    public void ClientUnsubscribed(IClient client)
    {
        throw new NotImplementedException();
    }

    public bool FlowPacketPathMatch(string path)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public interface IFlowRepository : IDataRepository<Guid, IFlow>;

public class FlowRepository : DataRepository<Guid, IFlow>, IFlowRepository
{
}

