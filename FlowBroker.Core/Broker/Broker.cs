using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Flows;
using Microsoft.Extensions.Logging;

namespace FlowBroker.Core.Broker;

public interface IBroker : IDisposable
{
    IServiceProvider ServiceProvider { get; }

    void Start();
    void Stop();
}

public class Broker : IBroker
{
    private readonly IClientRepository _clientRepository;
    private readonly IFlowRepository _flowRepository;

    private readonly ILogger<Broker> _logger;
    private readonly IFlowPacketRepository _packetRepository;

    public Broker(IServiceProvider serviceProvider, ILogger<Broker> logger,
        IClientRepository clientRepository, IFlowRepository flowRepository,
        IFlowPacketRepository packetRepository)
    {
        _logger = logger;
        _clientRepository = clientRepository;
        _flowRepository = flowRepository;
        _packetRepository = packetRepository;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public void Start()
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
