using Microsoft.Build.Framework;

namespace MSBuildBinaryLogAnalyzer.TraceEvents
{
    public interface IBuildEventArgsStrategy
    {
        string GetTraceEventName(BuildEventArgs args);
        bool IsStartedEventArgs(BuildEventArgs args);
        bool IsFinishedEventArgs(BuildEventArgs args);
        (int, int) GetBuildEventId(BuildEventArgs args);
        (int, int, string) GetBuildCodeObjectId(BuildEventArgs args);
    }
}
