using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Tcp;

public interface IListener : IDisposable
{
    event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;

    void Start();
    void Stop();
}

public sealed class TcpListener : IListener
{
    private readonly IPEndPoint _endPoint;
    private readonly ILogger<TcpListener> _logger;
    private bool _isAccepting;
    private bool _isDisposed;

    private Socket _socket;

    private SocketAsyncEventArgs _socketAsyncEventArgs;

    public TcpListener(IPEndPoint endPoint, ILogger<TcpListener> logger)
    {
        _endPoint = endPoint;
        _logger = logger;
    }

    public event EventHandler<SocketAcceptedEventArgs> OnSocketAccepted;

    public void Start()
    {
        ThrowIfDisposed();

        if (_isAccepting)
            throw new InvalidOperationException("Server is already accepting connection");

        _isAccepting = true;

        _socketAsyncEventArgs = new SocketAsyncEventArgs();
        _socketAsyncEventArgs.Completed += OnAcceptCompleted;

        _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(_endPoint);
        _socket.Listen(1024);

        _logger.LogInformation($"Started socket on endpoint {_endPoint}");

        BeginAcceptConnection();
    }

    public void Stop()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Stopping socket server");

        _isAccepting = false;

        _socketAsyncEventArgs.Completed -= OnAcceptCompleted;

        _socket.Close();
        _socketAsyncEventArgs.Dispose();

        Dispose();
    }

    public void Dispose()
    {
        _isDisposed = true;
    }

    private void BeginAcceptConnection()
    {
        try
        {
            _socketAsyncEventArgs.AcceptSocket = null;

            // accept while sync, break when we go async
            while (_isAccepting && !_socket.AcceptAsync(_socketAsyncEventArgs))
            {
                OnAcceptCompleted(null, _socketAsyncEventArgs);
                _socketAsyncEventArgs.AcceptSocket = null;
            }
        } catch (Exception e)
        {
            _logger.LogError($"Server encountered an exception while trying to accept connection, exception: {e}");
        }
    }

    private void OnAcceptCompleted(object _, SocketAsyncEventArgs socketAsyncEventArgs)
    {
        switch (socketAsyncEventArgs.SocketError)
        {
            case SocketError.Success:
                OnAcceptSuccess(socketAsyncEventArgs.AcceptSocket);
                break;
            default:
                OnAcceptError(socketAsyncEventArgs.SocketError);
                break;
        }

        BeginAcceptConnection();
    }

    private void OnAcceptSuccess(Socket socket)
    {
        _logger.LogInformation($"Accepted new socket connection from {socket.RemoteEndPoint}");

        var tcpSocket = new TcpSocket(socket);

        var socketAcceptedEventArgs = new SocketAcceptedEventArgs { Socket = tcpSocket };

        OnSocketAccepted?.Invoke(this, socketAcceptedEventArgs);
    }

    private void OnAcceptError(SocketError err)
    {
        _logger.LogError($"Failed to accept socket connection, error: {err}");
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException("Server has been disposed");
    }
}

public class SocketAcceptedEventArgs : System.EventArgs
{
    public ISocket Socket { get; set; }
}