using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Tcp;

public interface ISocket : IDisposable
{
    bool Connected { get; }
    void Disconnect();

    ValueTask<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken);
    ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
}

public sealed class TcpSocket : ISocket
{
    private readonly Socket _socket;

    private bool _disposed;

    public TcpSocket(Socket socket)
    {
        _socket = socket;
    }

    public bool Connected => _socket?.Connected ?? false;

    public void Disconnect()
    {
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        } catch (SocketException)
        {
            // ignore SocketException
        } catch (ObjectDisposedException)
        {
            // ignore ObjectDisposedException
        }

        // dispose the object
        Dispose();
    }

    public async ValueTask<int> SendAsync(Memory<byte> data, CancellationToken cancellationToken)
    {
        if (_disposed)
            return 0;

        try
        {
            return await _socket.SendAsync(data, SocketFlags.None, cancellationToken);
        } catch (TaskCanceledException)
        {
            return 0;
        } catch
        {
            Disconnect();
            return 0;
        }
    }

    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_disposed)
            return 0;

        try
        {
            return await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
        } catch (TaskCanceledException)
        {
            return 0;
        } catch
        {
            Disconnect();
            return 0;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
            try
            {
                _socket.Dispose();
                _disposed = true;
            } catch (ObjectDisposedException)
            {
                // ignore ObjectDisposedException
            }
    }

    public static TcpSocket NewFromEndPoint(EndPoint endPoint)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(endPoint);
        return new TcpSocket(socket);
    }
}