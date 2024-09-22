namespace FlowBroker.Core.PathMatching;

public interface IPathMatcher
{
    bool Match(string flowPacketPath, string flowPath);
}

internal class PathMatcher : IPathMatcher
{
    public bool Match(string flowPacketPath, string flowPath)
    {
        if (flowPacketPath is null || flowPath is null) return false;

        const string wildCard = "*";

        var flowPacketPathSegments = flowPacketPath.Split('/');
        var queuePathSegments = flowPath.Split('/');

        var minSegmentCount = Math.Min(flowPacketPathSegments.Length,
            queuePathSegments.Length);

        for (var i = 0; i < minSegmentCount; i++)
        {
            var flowPacketSegment = flowPacketPathSegments[i];
            var queueSegment = queuePathSegments[i];

            if (flowPacketSegment == wildCard || queueSegment == wildCard)
                continue;

            if (flowPacketSegment == queueSegment)
                continue;

            return false;
        }

        return true;
    }
}
