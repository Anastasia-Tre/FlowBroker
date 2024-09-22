using System.Buffers;
using System.Text;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Utils.Pooling;

namespace FlowBroker.Core.Serialization;

public class BinaryProtocolWriter : IDisposable, IPooledObject
{
    private byte[] _buffer;
    private int _currentBufferOffset;
    private Guid _id;
    private FlowPacketType _type;

    public BinaryProtocolWriter()
    {
        if (_buffer is null)
            _buffer = ArrayPool<byte>.Shared.Rent(64);

        Refresh();
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer, true);
    }

    public Guid PoolId { get; set; }

    public BinaryProtocolWriter WriteType(FlowPacketType type)
    {
        _type = type;
        return WriteInt((int)type);
    }

    public BinaryProtocolWriter WriteId(Guid id)
    {
        _id = id;

        const int requiredSizeForGuid = 16;

        MakeSureBufferSizeHasRoomForSize(requiredSizeForGuid);

        id.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset));

        _currentBufferOffset += requiredSizeForGuid;

        WriteNewLine();

        return this;
    }

    public BinaryProtocolWriter WriteInt(int i)
    {
        const int requiredSizeForInt = 4;

        MakeSureBufferSizeHasRoomForSize(requiredSizeForInt);

        BitConverter.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset), i);

        _currentBufferOffset += requiredSizeForInt;

        WriteNewLine();

        return this;
    }

    public BinaryProtocolWriter WriteStr(string s)
    {
        // note: we only check for ascii because strings used for queues can only be ascii

        MakeSureBufferSizeHasRoomForSize(s.Length);

        Encoding.UTF8.GetBytes(s, _buffer.AsSpan(_currentBufferOffset));

        _currentBufferOffset += s.Length;

        WriteNewLine();

        return this;
    }

    public BinaryProtocolWriter WriteMemory(Memory<byte> m)
    {
        MakeSureBufferSizeHasRoomForSize(m.Length +
                                         BinaryProtocolConfiguration
                                             .SizeForInt);

        BitConverter.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset),
            m.Length);

        m.CopyTo(_buffer.AsMemory(_currentBufferOffset +
                                  BinaryProtocolConfiguration.SizeForInt));

        _currentBufferOffset +=
            m.Length + BinaryProtocolConfiguration.SizeForInt;

        WriteNewLine();

        return this;
    }

    public SerializedPayload ToSerializedPayload()
    {
        try
        {
            WriteSizeOfPayloadToBufferHeader();

            var serializedPayload = ObjectPool.Shared.Get<SerializedPayload>();

            serializedPayload.FillFrom(_buffer, _currentBufferOffset, _id);

            return serializedPayload;
        }
        finally
        {
            Refresh();
        }
    }

    private void WriteSizeOfPayloadToBufferHeader()
    {
        var sizeOfPayload = _currentBufferOffset -
                            BinaryProtocolConfiguration.PayloadHeaderSize;
        BitConverter.TryWriteBytes(_buffer, sizeOfPayload);
    }

    private void WriteNewLine()
    {
        BitConverter.TryWriteBytes(_buffer.AsSpan(_currentBufferOffset), '\n');
        _currentBufferOffset += 1;
    }

    private void Refresh()
    {
        _currentBufferOffset = BinaryProtocolConfiguration.PayloadHeaderSize;
    }

    private void MakeSureBufferSizeHasRoomForSize(int s)
    {
        // + 1 for new line `\n`
        var exceedingSize = s - (_buffer.Length - _currentBufferOffset) + 1;

        if (exceedingSize > 0)
        {
            var newBuffer =
                ArrayPool<byte>.Shared.Rent(_buffer.Length + exceedingSize);
            _buffer.CopyTo(newBuffer.AsMemory());
            ArrayPool<byte>.Shared.Return(_buffer, true);
            _buffer = newBuffer;
        }
    }
}
