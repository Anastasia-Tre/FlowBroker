//using FlowBroker.Client.Payload;
//using FlowBroker.Core.FlowPackets;
//using FlowBroker.Core.Serialization;
//using FlowBroker.Core.Utils.BinarySerialization;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace FlowBroker.Test.Serialization;

//public class TestPacketData : IPacket
//{
//    public string TestString { get; set; }
//    public int TestInt { get; set; }
//}

//public class SerializationTests
//{
//    private readonly ISerializer _serializer;
//    private readonly IDeserializer _deserializer;

//    private readonly TestPacketData _packet1;
//    private readonly TestPacketData _packet2;

//    public SerializationTests()
//    {
//        _serializer = new Serializer();
//        _deserializer = new Deserializer();

//        _packet1 = new TestPacketData() { TestString = "test string", TestInt = 12345 };
//        _packet2 = new TestPacketData() { TestInt = 12345 };
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_ShouldReturnEquivalentFlowPacket()
//    {
//        // Arrange
//        var packet1 = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = "TestFlow",
//            FlowPath = "TestPath",
//            Priority = FlowPacketPriority.High,
//            DataType = typeof(TestPacketData),
//            Data = JsonHelper.ObjectToByteArray(_packet1),
//            PacketType = FlowPacketType.FlowPacket
//        };

//        var packet = FlowPacketFactory.NewPacket("FlowPath", _packet1);

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var payloadType = _deserializer.ParseFlowPacketType(serializedPayload.Data);
//        var deserializedPacket = _deserializer.Deserialized(payloadType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(packet.Id, deserializedPacket.Id);
//        Assert.Equal(packet.FlowName, deserializedPacket.FlowName);
//        Assert.Equal(packet.FlowPath, deserializedPacket.FlowPath);
//        Assert.Equal(packet.Priority, deserializedPacket.Priority);
//        Assert.Equal(packet.DataType, deserializedPacket.DataType);
//        Assert.Equal(packet.Data.ToArray(), deserializedPacket.Data.ToArray());
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_WithNullData_ShouldReturnEquivalentFlowPacket()
//    {
//        // Arrange
//        var packet = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = "TestFlow",
//            FlowPath = "TestPath",
//            Priority = FlowPacketPriority.Normal,
//            DataType = null,
//            Data = null,
//            PacketType = FlowPacketType.FlowPacket
//        };

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var deserializedPacket = _deserializer.Deserialized(packet.PacketType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(packet.Id, deserializedPacket.Id);
//        Assert.Equal(packet.FlowName, deserializedPacket.FlowName);
//        Assert.Equal(packet.FlowPath, deserializedPacket.FlowPath);
//        Assert.Equal(packet.Priority, deserializedPacket.Priority);
//        Assert.Null(deserializedPacket.DataType);
//        Assert.Null(deserializedPacket.Data.ToArray());
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_WithDifferentDataType_ShouldWorkCorrectly()
//    {
//        // Arrange
//        var packet = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = "DataFlow",
//            FlowPath = "DataPath",
//            Priority = FlowPacketPriority.Normal,
//            DataType = typeof(int),
//            Data = BitConverter.GetBytes(12345),
//            PacketType = FlowPacketType.Configure
//        };

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var deserializedPacket = _deserializer.Deserialized(packet.PacketType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(packet.Id, deserializedPacket.Id);
//        Assert.Equal(packet.FlowName, deserializedPacket.FlowName);
//        Assert.Equal(packet.FlowPath, deserializedPacket.FlowPath);
//        Assert.Equal(packet.DataType, deserializedPacket.DataType);
//        Assert.Equal(packet.Data.ToArray(), deserializedPacket.Data.ToArray());
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_WithEmptyFlowName_ShouldHandleCorrectly()
//    {
//        // Arrange
//        var packet = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = null,  // No flow name provided
//            FlowPath = "TestPath",
//            Priority = FlowPacketPriority.Normal,
//            DataType = typeof(string),
//            Data = new byte[] { 0x04, 0x05, 0x06 },
//            PacketType = FlowPacketType.FlowDelete
//        };

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var deserializedPacket = _deserializer.Deserialized(packet.PacketType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(packet.Id, deserializedPacket.Id);
//        Assert.Null(deserializedPacket.FlowName); // FlowName should still be null after deserialization
//        Assert.Equal(packet.FlowPath, deserializedPacket.FlowPath);
//        Assert.Equal(packet.DataType, deserializedPacket.DataType);
//        Assert.Equal(packet.Data.ToArray(), deserializedPacket.Data.ToArray());
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_WithEmptyFlowPath_ShouldHandleCorrectly()
//    {
//        // Arrange
//        var packet = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = "TestFlow",
//            FlowPath = null,  // No flow path provided
//            Priority = FlowPacketPriority.Normal,
//            DataType = typeof(string),
//            Data = new byte[] { 0x07, 0x08, 0x09 },
//            PacketType = FlowPacketType.SubscribeFlow
//        };

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var deserializedPacket = _deserializer.Deserialized(packet.PacketType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(packet.Id, deserializedPacket.Id);
//        Assert.Equal(packet.FlowName, deserializedPacket.FlowName);
//        Assert.Null(deserializedPacket.FlowPath); // FlowPath should still be null after deserialization
//        Assert.Equal(packet.DataType, deserializedPacket.DataType);
//        Assert.Equal(packet.Data.ToArray(), deserializedPacket.Data.ToArray());
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_WithEmptyDataType_ShouldHandleCorrectly()
//    {
//        // Arrange
//        var packet = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = "TestFlow",
//            FlowPath = "TestPath",
//            Priority = FlowPacketPriority.Normal,
//            DataType = null,  // No DataType provided
//            Data = new byte[] { 0x01, 0x02, 0x03 },
//            PacketType = FlowPacketType.Error
//        };

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var deserializedPacket = _deserializer.Deserialized(packet.PacketType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(packet.Id, deserializedPacket.Id);
//        Assert.Equal(packet.FlowName, deserializedPacket.FlowName);
//        Assert.Equal(packet.FlowPath, deserializedPacket.FlowPath);
//        Assert.Null(deserializedPacket.DataType); // DataType should be null after deserialization
//        Assert.Equal(packet.Data.ToArray(), deserializedPacket.Data.ToArray());
//    }

//    [Fact]
//    public void Deserialize_WithCorruptedData_ShouldThrowException()
//    {
//        // Arrange
//        var invalidData = new byte[] { 0xFF, 0xAA, 0xBB }; // Invalid/corrupt data
//        var memory = new Memory<byte>(invalidData);

//        // Act & Assert
//        Assert.Throws<Exception>(() => _deserializer.Deserialized(FlowPacketType.FlowPacket, memory));
//    }

//    [Fact]
//    public void Serialize_And_Deserialize_WithCriticalPriority_ShouldReturnCorrectPriority()
//    {
//        // Arrange
//        var packet = new FlowPacket
//        {
//            Id = Guid.NewGuid(),
//            FlowName = "CriticalFlow",
//            FlowPath = "CriticalPath",
//            Priority = FlowPacketPriority.Critical,  // Setting priority to Critical
//            DataType = typeof(string),
//            Data = new byte[] { 0xAA, 0xBB, 0xCC },
//            PacketType = FlowPacketType.Ack
//        };

//        // Act
//        var serializedPayload = _serializer.Serialize(packet);
//        var deserializedPacket = _deserializer.Deserialized(packet.PacketType, serializedPayload.Data);

//        // Assert
//        Assert.Equal(FlowPacketPriority.Critical, deserializedPacket.Priority); // Priority should be Critical
//    }
//}