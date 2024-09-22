using FlowBroker.Core.Utils.Pooling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowBroker.Core.Utils.Buffer;

namespace FlowBroker.Core.Payload;

public interface IBinaryDataProcessor : IDisposable
{
    void Write(Memory<byte> chunk);

    void BeginLock();

    void EndLock();

    bool TryRead(out BinaryPayload binaryPayload);
}


public class BinaryDataProcessor : IBinaryDataProcessor
{
    private readonly DynamicBuffer _dynamicBuffer;
    private bool _disposed;
    private bool _isReading;

    public BinaryDataProcessor()
    {
        _dynamicBuffer = new DynamicBuffer();
    }

    public void Write(Memory<byte> chunk)
    {
        _dynamicBuffer.Write(chunk);
    }

    public void BeginLock()
    {
        _isReading = true;
    }

    public void EndLock()
    {
        _isReading = false;
    }

    public bool TryRead(out BinaryPayload binaryPayload)
    {
        lock (_dynamicBuffer)
        {
            binaryPayload = null;

            if (_disposed) return false;

            var canReadHeaderSize = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize);

            if (!canReadHeaderSize) return false;

            var headerSizeBytes = _dynamicBuffer.Read(BinaryProtocolConfiguration.PayloadHeaderSize);
            var headerSize = BitConverter.ToInt32(headerSizeBytes);

            var canReadPayload = _dynamicBuffer.CanRead(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

            if (!canReadPayload) return false;

            var payload = _dynamicBuffer.ReadAndClear(BinaryProtocolConfiguration.PayloadHeaderSize + headerSize);

            var receiveDataBuffer = ArrayPool<byte>.Shared.Rent(payload.Length);

            payload.CopyTo(receiveDataBuffer);

            binaryPayload = ObjectPool.Shared.Get<BinaryPayload>();


            binaryPayload.Setup(receiveDataBuffer, payload.Length);

            return true;
        }
    }

    public void Dispose()
    {
        while (_isReading) Thread.Yield();

        if (_disposed) return;

        lock (_dynamicBuffer)
        {
            _disposed = true;
            _dynamicBuffer.Dispose();
        }
    }
}
