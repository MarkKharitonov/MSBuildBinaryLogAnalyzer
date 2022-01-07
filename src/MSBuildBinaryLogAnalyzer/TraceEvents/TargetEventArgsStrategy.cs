using Microsoft.Build.Framework;

namespace MSBuildBinaryLogAnalyzer.TraceEvents
{
    public class TargetEventArgsStrategy : IBuildEventArgsStrategy
    {
        private readonly string m_targetName;

        public TargetEventArgsStrategy(string targetName)
        {
            m_targetName = targetName;
        }

        public string GetTraceEventName(BuildEventArgs args) => ((TargetStartedEventArgs)args).ProjectName();
        public bool IsStartedEventArgs(BuildEventArgs args) => args is TargetStartedEventArgs o && o.TargetName == m_targetName;
        public bool IsFinishedEventArgs(BuildEventArgs args) => args is TargetFinishedEventArgs o && o.TargetName == m_targetName;
        public (int, int) GetBuildEventId(BuildEventArgs args) => (args.BuildEventContext.NodeId, args.BuildEventContext.TargetId);
        public (int, int, string) GetBuildCodeObjectId(BuildEventArgs args) => (args.BuildEventContext.NodeId, args.BuildEventContext.ProjectInstanceId, m_targetName);
    }
}
