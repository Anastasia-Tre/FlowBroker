using FlowBroker.Core.Clients;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Flows;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Tcp;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IListener _listener;
    private readonly ILogger<Broker> _logger;
    private readonly IFlowPacketRepository _flowPacketRepository;
    private readonly IPayloadProcessor _payloadProcessor;
    private readonly IFlowRepository _flowRepository;
    private bool _disposed;

    public Broker(IListener listener, IPayloadProcessor payloadProcessor, IClientRepository clientRepository,
        IFlowRepository flowRepository,
        IFlowPacketRepository flowPacketRepository, IServiceProvider serviceProvider, ILogger<Broker> logger)
    {
        _listener = listener;
        _payloadProcessor = payloadProcessor;
        _clientRepository = clientRepository;
        _flowRepository = flowRepository;
        _flowPacketRepository = flowPacketRepository;
        _logger = logger;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public void Start()
    {
        _listener.OnSocketAccepted += ClientConnected;

        _listener.Start();
        _flowRepository.Setup();
        _flowPacketRepository.Setup();
    }

    public void Stop()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _listener.OnSocketAccepted -= ClientConnected;
            _listener.Stop();
            _disposed = true;
        }
    }

    private void ClientConnected(object _, SocketAcceptedEventArgs eventArgs)
    {
        try
        {
            var clientSession = ServiceProvider.GetRequiredService<IClient>();

            clientSession.Setup(eventArgs.Socket);

            clientSession.OnDisconnected += ClientDisconnected;
            clientSession.OnDataReceived += ClientDataReceived;

            // must add the socket to client store before calling StartReceiveProcess 
            // otherwise we might receive flowPackets before having access to client in client store
            _clientRepository.Add(clientSession.Id, clientSession);

            clientSession.StartReceiveProcess();
            clientSession.StartSendProcess();

            _logger.LogInformation($"Client: {clientSession.Id} connected");
        } catch (ObjectDisposedException)
        {
        }
    }

    private void ClientDisconnected(object clientSession, ClientSessionDisconnectedEventArgs eventArgs)
    {
        if (clientSession is IClient client)
        {
            _logger.LogInformation($"Client: {client.Id} removed");

            foreach (var queue in _flowRepository.GetAll())
                queue.ClientUnsubscribed(client);

            _clientRepository.Remove(client.Id);
        }
    }

    private void ClientDataReceived(object clientSession, ClientSessionDataReceivedEventArgs eventArgs)
    {
        try
        {
            _payloadProcessor.OnDataReceived(eventArgs.Id, eventArgs.Data);
        } catch (Exception e)
        {
            _logger.LogError($"An error occured while trying to dispatch flowPackets, error: {e}");
        }
    }
}
