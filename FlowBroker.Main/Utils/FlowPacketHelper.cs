using System.Collections.Concurrent;
using FlowBroker.Client.Payload;
using FlowBroker.Core.FlowPackets;

namespace FlowBroker.Main.Utils;

internal class FlowPacketHelper
{
    private readonly ConcurrentDictionary<Guid, FlowPacket> _allFlowPackets;
    private readonly ConcurrentDictionary<Guid, bool> _receivedFlowPackets;

    private readonly ConcurrentDictionary<Guid, bool> _sentFlowPackets;
    private string _defaultFlowPath;

    private int _numberOfFlowPackets;

    public FlowPacketHelper()
    {
        _allFlowPackets = new ConcurrentDictionary<Guid, FlowPacket>();
        _receivedFlowPackets = new ConcurrentDictionary<Guid, bool>();
        _sentFlowPackets = new ConcurrentDictionary<Guid, bool>();
    }

    public int ReceivedCount => _receivedFlowPackets.Count;
    public int SentCount => _sentFlowPackets.Count;

    public void Setup(string defaultFlowPath, int numberOfFlowPackets)
    {
        _defaultFlowPath = defaultFlowPath;
        _numberOfFlowPackets = numberOfFlowPackets;
    }

    public (FlowPacket, WorkflowPacket) NewFlowPacket(string path = null)
    {
        var id = Guid.NewGuid();
        var workflowPacket = new WorkflowPacket
        {
            Data = id
        };

        var flowPacket = FlowPacketFactory.NewPacket(path, workflowPacket);
        flowPacket.Id = id;

        _allFlowPackets[id] = flowPacket;

        return (flowPacket, workflowPacket);
    }

    public void OnFlowPacketSent(Guid id)
    {
        // check if flowPacket id is valid
        if (_allFlowPackets.ContainsKey(id))
            _sentFlowPackets[id] = true;
        else
            throw new Exception("Invalid flowPacket is was provided");
    }

    public void OnFlowPacketReceived(Guid id)
    {
        Console.WriteLine($"!!!!!!!!!!!!!!!!!!!! {id}");
        if (_allFlowPackets.ContainsKey(id))
            _receivedFlowPackets[id] = true;
        else
            throw new Exception("Invalid flowPacket is was provided");
    }

    public void WaitForAllFlowPacketToBeReceived()
    {
        var lastTimeCheck = -1;
        while (true)
        {
            // if everything is ok
            if (ReceivedCount == _numberOfFlowPackets) break;

            Thread.Sleep(10000);

            // if the number of received count hasn't changed then print the flowPackets
            if (ReceivedCount <= lastTimeCheck)
            {
                foreach (var (key, _) in _allFlowPackets)
                    if (!_receivedFlowPackets.ContainsKey(key))
                        Console.WriteLine(
                            $"FlowPacket {key} was not received by the subscription");

                throw new Exception(
                    $"Number of received flowPackets is {ReceivedCount} but should be {_numberOfFlowPackets}");
            }

            lastTimeCheck = ReceivedCount;
        }

        Console.WriteLine("All packets are received...");
    }

    public void WaitForAllFlowPacketToBeSent()
    {
        var lastTimeCheck = 0;
        while (true)
        {
            // if everything is ok
            if (SentCount == _numberOfFlowPackets) break;

            Thread.Sleep(1000);

            // if the number of sent count hasn't changed then print the flowPackets
            if (SentCount <= lastTimeCheck)
            {
                foreach (var (key, _) in _allFlowPackets)
                    if (!_sentFlowPackets.ContainsKey(key))
                        Console.WriteLine(
                            $"FlowPacket {key} was not received by the subscription");

                throw new Exception(
                    $"Number of received flowPackets is {SentCount} but should be {_numberOfFlowPackets}");
            }

            lastTimeCheck = SentCount;
        }

        Console.WriteLine("All packets are sent...");
    }
}

internal class WorkflowPacket : IPacket
{
    public Guid Data { get; set; }
}
