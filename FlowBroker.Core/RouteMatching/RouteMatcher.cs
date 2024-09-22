using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowBroker.Core.RouteMatching;

public interface IRouteMatcher
{
    bool Match(string flowPacketRoute, string flowRoute);
}

internal class RouteMatcher : IRouteMatcher
{
    public bool Match(string flowPacketRoute, string flowRoute)
    {
        if (flowPacketRoute is null || flowRoute is null) return false;

        const string wildCard = "*";

        var flowPacketRouteSegments = flowPacketRoute.Split('/');
        var queueRouteSegments = flowRoute.Split('/');

        var minSegmentCount = Math.Min(flowPacketRouteSegments.Length, queueRouteSegments.Length);

        for (var i = 0; i < minSegmentCount; i++)
        {
            var flowPacketSegment = flowPacketRouteSegments[i];
            var queueSegment = queueRouteSegments[i];

            if (flowPacketSegment == wildCard || queueSegment == wildCard)
                continue;

            if (flowPacketSegment == queueSegment)
                continue;

            return false;
        }

        return true;
    }
}
