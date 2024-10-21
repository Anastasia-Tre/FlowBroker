using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FlowBroker.Core.Tcp;
using TcpListener = FlowBroker.Core.Tcp.TcpListener;

namespace FlowBroker.Test.Tcp;

public class ListenerTests
{
    [Fact]
    public void ListenForIncomingConnection_WithValidClient_AcceptConnectionAndNotify()
    {
        var endPoint = new IPEndPoint(IPAddress.Loopback, 8100);
        var connection = new ConnectionProvider() { IpEndPoint = endPoint };
        var listener = new TcpListener(connection, NullLogger<TcpListener>.Instance);
        listener.Start();

        var socketWasAccepted = false;
        var manualResetEvent = new ManualResetEvent(false);

        listener.OnSocketAccepted += (_, args) =>
        {
            manualResetEvent.Set();
            socketWasAccepted = true;
        };

        var client = new TcpClient();

        client.Connect(connection.IpEndPoint);

        manualResetEvent.WaitOne(TimeSpan.FromSeconds(10));

        Assert.True(socketWasAccepted);
    }
}
