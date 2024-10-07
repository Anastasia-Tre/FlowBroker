namespace FlowBroker.Core.PathMatching;

public interface IPathMatcher
{
    const string WildCard = "*";
    const string PathSeparator = "/";

    bool Match(string flowPacketPath, string flowPath);
}

public class DefaultPathMatcher : IPathMatcher
{
    public bool Match(string flowPacketPath, string flowPath)
    {
        if (string.IsNullOrEmpty(flowPacketPath) || string.IsNullOrEmpty(flowPath))
            return false;

        var flowPacketPathSegments = flowPacketPath.Split(IPathMatcher.PathSeparator);
        var queuePathSegments = flowPath.Split(IPathMatcher.PathSeparator);

        var minSegmentCount = Math.Min(flowPacketPathSegments.Length,
            queuePathSegments.Length);

        for (var i = 0; i < minSegmentCount; i++)
        {
            var flowPacketSegment = flowPacketPathSegments[i];
            var queueSegment = queuePathSegments[i];

            if (flowPacketSegment == IPathMatcher.WildCard || queueSegment == IPathMatcher.WildCard)
                continue;

            if (flowPacketSegment == queueSegment)
                continue;

            return false;
        }

        return true;
    }
}
