using FlowBroker.Core.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Serialization;

namespace FlowBroker.Client.Payload;

public interface IPayloadFactory
{
    SerializedPayload NewPacket(FlowPacketType type, string path, byte[] data = null);
    SerializedPayload NewPacket(FlowPacketType type, string path, string data);
}


public class PayloadFactory : IPayloadFactory
{
    private readonly ISerializer _serializer;

    public PayloadFactory(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public SerializedPayload NewPacket(FlowPacketType type, string path,byte[] data = null)
    {
        var payload = new FlowPacket()
        {
            Id = Guid.NewGuid(),
            PacketType = type,
            Data = data,
            FlowPath = path
        };

        return _serializer.Serialize(payload);
    }

    public SerializedPayload NewPacket(FlowPacketType type, string path, string data)
    {
        return NewPacket(type, path, Encoding.ASCII.GetBytes(data));
    }
}
