using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowBroker.Core.FlowPackets;
using FlowBroker.Core.Payload;
using FlowBroker.Core.Serialization;

namespace FlowBroker.Main.Utils;

internal static class RandomGenerator
{
    public static string GenerateString(int length, Random random = null)
    {
        random ??= new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static byte[] GenerateBytes(int length, Random random = null)
    {
        return Encoding.UTF8.GetBytes(GenerateString(length, random));
    }

    public static FlowPacket GetFlowPacket(string path)
    {
        return new()
        {
            Data = Encoding.UTF8.GetBytes(GenerateString(10)),
            Id = Guid.NewGuid(),
            FlowPath = path ?? GenerateString(10)
        };
    }

    public static SerializedPayload GetFlowPacketSerializedPayload(string path = default)
    {
        var serializer = new Serializer();
        return serializer.Serialize(GetFlowPacket(path));
    }

    public static double GenerateDouble()
    {
        var random = new Random();
        return random.NextDouble();
    }
}