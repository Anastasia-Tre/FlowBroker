using FlowBroker.Core.Utils.Pooling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.Payload;

public class BinaryPayload : IPooledObject
{
    private byte[] _data;
    private int _size;

    public BinaryPayload()
    {
        PoolId = Guid.NewGuid();
    }

    public Memory<byte> DataWithoutSize => _data.AsMemory(BinaryProtocolConfiguration.PayloadHeaderSize,
        _size - BinaryProtocolConfiguration.PayloadHeaderSize);

    public Guid PoolId { get; set; }


    public void Setup(byte[] data, int size)
    {
        _data = data;
        _size = size;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_data);
    }
}
