using FlowBroker.Core.FlowPackets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FlowBroker.Test.PacketPrioritization;

public class FlowPacketRepositoryTests
{
    private readonly FlowPacketRepository _repository;

    public FlowPacketRepositoryTests()
    {
        _repository = new FlowPacketRepository();
    }

    [Fact]
    public void Add_NewPacket_ShouldAddSuccessfully()
    {
        // Arrange
        var packet = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Normal);

        // Act
        _repository.Add(packet.Id, packet);

        // Assert
        Assert.True(_repository.TryGet(packet.Id, out var retrievedPacket));
        Assert.Equal(packet, retrievedPacket);
    }

    [Fact]
    public void Add_DuplicatePacket_ShouldThrowException()
    {
        // Arrange
        var packetId = Guid.NewGuid();
        var packet = CreateFlowPacket(packetId, FlowPacketPriority.Normal);

        // Act
        _repository.Add(packetId, packet);

        // Assert
        Assert.Throws<ArgumentException>(() => _repository.Add(packetId, packet));
    }

    [Fact]
    public void TryGet_ExistingPacket_ShouldReturnTrue()
    {
        // Arrange
        var packet = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.High);
        _repository.Add(packet.Id, packet);

        // Act
        var result = _repository.TryGet(packet.Id, out var retrievedPacket);

        // Assert
        Assert.True(result);
        Assert.Equal(packet, retrievedPacket);
    }

    [Fact]
    public void TryGet_NonExistingPacket_ShouldReturnFalse()
    {
        // Arrange
        var packetId = Guid.NewGuid();

        // Act
        var result = _repository.TryGet(packetId, out var packet);

        // Assert
        Assert.False(result);
        Assert.Null(packet);
    }

    [Fact]
    public void Remove_ExistingPacket_ShouldReturnTrue()
    {
        // Arrange
        var packet = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Normal);
        _repository.Add(packet.Id, packet);

        // Act
        var result = _repository.Remove(packet.Id);

        // Assert
        Assert.True(result);
        Assert.False(_repository.TryGet(packet.Id, out _));
    }

    [Fact]
    public void Remove_NonExistingPacket_ShouldReturnFalse()
    {
        // Arrange
        var packetId = Guid.NewGuid();

        // Act
        var result = _repository.Remove(packetId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNextPacket_ShouldReturnHighestPriorityPacket()
    {
        // Arrange
        var lowPriorityPacket = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Low);
        var criticalPriorityPacket = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Critical);

        _repository.Add(lowPriorityPacket.Id, lowPriorityPacket);
        _repository.Add(criticalPriorityPacket.Id, criticalPriorityPacket);

        // Act
        var nextPacket = _repository.GetNextPacket();

        // Assert
        Assert.Equal(criticalPriorityPacket, nextPacket);
    }

    [Fact]
    public void GetNextPacket_EmptyRepository_ShouldReturnNull()
    {
        // Act
        var nextPacket = _repository.GetNextPacket();

        // Assert
        Assert.Null(nextPacket);
    }

    [Fact]
    public void GetAll_ShouldReturnPacketsInPriorityOrder()
    {
        // Arrange
        var lowPriorityPacket = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Low);
        var highPriorityPacket = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.High);
        var normalPriorityPacket = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Normal);

        _repository.Add(lowPriorityPacket.Id, lowPriorityPacket);
        _repository.Add(highPriorityPacket.Id, highPriorityPacket);
        _repository.Add(normalPriorityPacket.Id, normalPriorityPacket);

        // Act
        var allPackets = _repository.GetAll().ToList();

        // Assert
        Assert.Collection(allPackets,
            packet => Assert.Equal(FlowPacketPriority.High, packet.Priority),
            packet => Assert.Equal(FlowPacketPriority.Normal, packet.Priority),
            packet => Assert.Equal(FlowPacketPriority.Low, packet.Priority));
    }

    [Fact]
    public void AddPacket_ToSendFlowPacket_ShouldReturnNewFlowPacket()
    {
        // Arrange
        var packet = CreateFlowPacket(Guid.NewGuid(), FlowPacketPriority.Normal);

        // Act
        var newPacket = packet.ToSendFlowPacket("NewFlow");

        // Assert
        Assert.NotEqual(packet.Id, newPacket.Id);
        Assert.Equal(packet.DataType, newPacket.DataType);
        Assert.Equal("NewFlow", newPacket.FlowName);
    }

    // Helper method to create FlowPacket instances
    private FlowPacket CreateFlowPacket(Guid id, FlowPacketPriority priority)
    {
        return new FlowPacket
        {
            Id = id,
            FlowName = "TestFlow",
            FlowPath = "/test/path",
            Priority = priority,
            DataType = typeof(string),
            Data = new byte[] { 0x01, 0x02, 0x03 }.AsMemory(),
            OriginalFlowPacketData = new byte[] { 0x01, 0x02, 0x03 },
            PacketType = FlowPacketType.FlowPacket
        };
    }
}

