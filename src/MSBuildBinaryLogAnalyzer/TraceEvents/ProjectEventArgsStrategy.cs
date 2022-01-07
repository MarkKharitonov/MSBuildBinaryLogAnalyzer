using Microsoft.Build.Framework;

namespace MSBuildBinaryLogAnalyzer.TraceEvents
{
    public class ProjectEventArgsStrategy : IBuildEventArgsStrategy
    {
        public string GetTraceEventName(BuildEventArgs args) => ((ProjectStartedEventArgs)args).ProjectName();
        public bool IsStartedEventArgs(BuildEventArgs args) => args is ProjectStartedEventArgs o && o.IsActualProjectBuildEvent();
        public bool IsFinishedEventArgs(BuildEventArgs args) => args is ProjectFinishedEventArgs;
        public (int, int) GetBuildEventId(BuildEventArgs args) => (args.BuildEventContext.NodeId, args.BuildEventContext.ProjectContextId);
        public (int, int, string) GetBuildCodeObjectId(BuildEventArgs args) => (args.BuildEventContext.NodeId, args.BuildEventContext.ProjectInstanceId, null);
    }
}
