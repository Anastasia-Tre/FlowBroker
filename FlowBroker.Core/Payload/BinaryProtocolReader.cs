using System.Buffers;
using FlowBroker.Core.Utils.Pooling;

namespace FlowBroker.Core.Payload;

public class BinaryProtocolReader : IPooledObject
{
    private int _currentOffset;
    private Memory<byte> _receivedData;

    public Guid PoolId { get; set; }

    public void Setup(Memory<byte> data)
    {
        // skip the type 
        _currentOffset = BinaryProtocolConfiguration.SizeForInt +
                         BinaryProtocolConfiguration.SizeForNewLine;
        _receivedData = data;
    }

    public Guid ReadNextGuid()
    {
        try
        {
            var data = _receivedData.Span.Slice(_currentOffset,
                BinaryProtocolConfiguration.SizeForGuid);
            _currentOffset += BinaryProtocolConfiguration.SizeForGuid +
                              BinaryProtocolConfiguration.SizeForNewLine;
            return new Guid(data);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(
                $"Cannot read Guid, current is {_currentOffset}, needed is {BinaryProtocolConfiguration.SizeForGuid}, available is {_receivedData.Length - _currentOffset}");
        }
    }

    public string ReadNextString()
    {
        try
        {
            var data = _receivedData.Span[_currentOffset..];
            var indexOfDelimiter = data.IndexOf((byte)'\n');
            _currentOffset += indexOfDelimiter + 1;
            return StringPool.Shared.GetStringForBytes(
                data[..indexOfDelimiter]);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(
                $"Cannot read string, current is {_currentOffset}, available is {_receivedData.Length - _currentOffset}");
        }
    }

    public int ReadNextInt()
    {
        var data = _receivedData.Span.Slice(_currentOffset,
            BinaryProtocolConfiguration.SizeForInt);
        _currentOffset += BinaryProtocolConfiguration.SizeForInt +
                          BinaryProtocolConfiguration.SizeForNewLine;
        return BitConverter.ToInt32(data);
    }

    public (byte[] OriginalData, int Size) ReadNextBytes()
    {
        try
        {
            var spanForSizeOfBinaryData =
                _receivedData.Span.Slice(_currentOffset,
                    BinaryProtocolConfiguration.SizeForInt);

            var sizeToRead = BitConverter.ToInt32(spanForSizeOfBinaryData);

            _currentOffset += BinaryProtocolConfiguration.SizeForInt;

            var data = _receivedData.Span.Slice(_currentOffset, sizeToRead);

            _currentOffset += sizeToRead + 1;

            var arr = ArrayPool<byte>.Shared.Rent(sizeToRead);

            data.CopyTo(arr);

            return (arr, sizeToRead);
        }
        catch (Exception)
        {
            throw new ArgumentOutOfRangeException(
                $"Cannot read bytes, current is {_currentOffset}, available is {_receivedData.Length - _currentOffset}");
        }
    }
}
