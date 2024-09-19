using FlowBroker.Core.Utils.Pooling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Payload;

public class SerializedPayload : IPooledObject
{
    private byte[] _buffer;
    private int _size;

    public Guid PayloadId { get; private set; }

    public Memory<byte> Data => _buffer.AsMemory(0, _size);

    public Memory<byte> DataWithoutSize => Data[BinaryProtocolConfiguration.PayloadHeaderSize..];

    public Guid PoolId { get; set; }

    public void FillFrom(byte[] data, int size, Guid id)
    {
        if ((_buffer?.Length ?? 0) < size)
        {
            if (_buffer != null)
                ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = ArrayPool<byte>.Shared.Rent(size);
        }

        data.AsMemory(0, size).CopyTo(_buffer.AsMemory());
        _size = size;

        PayloadId = id;
    }
}
