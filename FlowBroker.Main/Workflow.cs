﻿using System.Net;
using FlowBroker.Client.BrokerClient;
using FlowBroker.Core.Broker;
using FlowBroker.Core.PathMatching;
using FlowBroker.Main.Utils;

namespace FlowBroker.Main;

public class Workflow<TPathMather>
    where TPathMather : class, IPathMatcher
{
    private readonly int _flowPacketCount;

    public Workflow(int flowPacketCount = 10)
    {
        _flowPacketCount = flowPacketCount;
    }

    public async Task StartProcess()
    {
        var topicName = RandomGenerator.GenerateString(10);
        var flowPacketStore = new FlowPacketHelper();
        var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 8100);

        // setup flowPacket store
        flowPacketStore.Setup(topicName, _flowPacketCount);

        // setup server
        using var broker = new BrokerBuilder()
            .AddConsoleLog()
            .UseEndPoint(serverEndPoint)
            .UsePathMatcher<TPathMather>()
            .Build();

        broker.Start();

        var clientFactory = new BrokerClientFactory();

        // setup publisher
        await using var publisherClient = clientFactory.GetClient();
        publisherClient.Connect(serverEndPoint);

        // setup subscriber
        await using var subscriberClient = clientFactory.GetClient();
        subscriberClient.Connect(serverEndPoint);

        // declare topic
        var declareResult =
            await publisherClient.DeclareFlowAsync(topicName, topicName);
        if (!declareResult.IsSuccess)
        {
            Console.WriteLine(declareResult.InternalErrorCode);
            throw new Exception(declareResult.InternalErrorCode);
        }

        // create subscription
        var subscription =
            await subscriberClient.GetFlowSubscriptionAsync(topicName);

        subscription.AddPacketHandler(typeof(WorkflowPacket), msg =>
        {
            var data = ((WorkflowPacket)msg.Data).Data;
            var flowPacketIdentifier = data;

            flowPacketStore.OnFlowPacketReceived(flowPacketIdentifier);

            msg.Ack();
        });

        // send flowPackets to server
        while (flowPacketStore.SentCount < _flowPacketCount)
        {
            var (flowPacket, workflowPacket) = flowPacketStore.NewFlowPacket();

            var cancellationTokenSource =
                new CancellationTokenSource(TimeSpan.FromSeconds(1));

            var publishResult = await publisherClient.PublishAsync(topicName,
                workflowPacket, cancellationTokenSource.Token);

            if (publishResult.IsSuccess)
                flowPacketStore.OnFlowPacketSent(flowPacket.Id);
            else
                Console.WriteLine(publishResult.InternalErrorCode);
        }

        // wait for flowPackets to be sent
        flowPacketStore.WaitForAllFlowPacketToBeSent();

        // wait for flowPackets to be received
        flowPacketStore.WaitForAllFlowPacketToBeReceived();
    }
}
