using System.Text.RegularExpressions;

namespace FlowBroker.Core.PathMatching;

public class RegexPathMatcher : IPathMatcher
{
    public bool Match(string flowPacketPath, string flowPath)
    {
        if (string.IsNullOrEmpty(flowPacketPath) ||
            string.IsNullOrEmpty(flowPath))
            return false;

        var result = MatchPaths(flowPacketPath, flowPath);
        return result;
    }

    private bool MatchPaths(string flowPacketPath, string flowPath)
    {
        var flowPacketPathSegments =
            flowPacketPath.Split(IPathMatcher.PathSeparator);
        var flowPathSegments = flowPath.Split(IPathMatcher.PathSeparator);

        var minSegmentCount = Math.Min(flowPacketPathSegments.Length,
            flowPathSegments.Length);

        for (var i = 0; i < minSegmentCount; i++)
        {
            var flowPacketSegment = flowPacketPathSegments[i];
            var flowSegment = flowPathSegments[i];

            if (flowSegment == IPathMatcher.WildCard ||
                flowPacketSegment == IPathMatcher.WildCard)
                continue;

            if (IsRegex(flowSegment))
            {
                var regexPattern = ExtractRegex(flowSegment);
                if (!Regex.IsMatch(flowPacketSegment, regexPattern))
                    return false;
            }
            else if (IsRegex(flowPacketSegment))
            {
                var regexPattern = ExtractRegex(flowPacketSegment);
                if (!Regex.IsMatch(flowSegment, regexPattern))
                    return false;
            }
            else
            {
                if (!flowPacketSegment.Equals(flowSegment,
                        StringComparison.OrdinalIgnoreCase)) return false;
            }
        }

        return true;
    }

    private bool IsRegex(string segment)
    {
        return segment.StartsWith("{") && segment.EndsWith("}");
    }

    private string ExtractRegex(string segment)
    {
        return segment.Substring(1, segment.Length - 2);
    }
}
