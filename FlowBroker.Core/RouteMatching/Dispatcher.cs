using FlowBroker.Core.Clients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.RouteMatching;

public interface IDispatcher
{
    void Add(IClient client);
    bool Remove(IClient client);

    IClient NextAvailable();
}

internal class Dispatcher : IDispatcher
{
    private readonly ConcurrentDictionary<Guid, IClient> _clients;
    private readonly ReaderWriterLockSlim _wrLock;

    public Dispatcher()
    {
        _clients = new ConcurrentDictionary<Guid, IClient>();
        _wrLock = new ReaderWriterLockSlim();
    }

    public void Add(IClient client)
    {
        try
        {
            _wrLock.EnterWriteLock();

            if (_clients.Keys.Any(sendQueueId => sendQueueId == client.Id))
                throw new Exception("Added SendQueue already exists");

            _clients[client.Id] = client;
        } finally
        {
            _wrLock.ExitWriteLock();
        }
    }

    public bool Remove(IClient client)
    {
        try
        {
            _wrLock.EnterWriteLock();
            return _clients.Remove(client.Id, out _);
        } finally
        {
            _wrLock.ExitWriteLock();
        }
    }

    public IClient NextAvailable()
    {
        try
        {
            _wrLock.EnterReadLock();

            foreach (var (_, client) in _clients) 
                return client;

            return null;
        } finally
        {
            _wrLock.ExitReadLock();
        }
    }
}
