using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MSBuildBinaryLogAnalyzer.TraceEvents
{
    public class InitialTraceEventProducer : IMSBuildNodeTraceEventProcessor
    {
        private List<List<TraceEvent>> m_msbuildNodes = new();

        public InitialTraceEventProducer(IList<BuildEventArgs> argsList, IBuildEventArgsStrategy strategy)
        {
            var firstObservedTime = argsList[0].Timestamp;
            var finishedEventArgs = argsList.Where(strategy.IsFinishedEventArgs).ToDictionary(strategy.GetBuildEventId);

            foreach (var g in argsList.Where(strategy.IsStartedEventArgs).GroupBy(strategy.GetBuildCodeObjectId))
            {
                var maxDuration = TimeSpan.MinValue;
                BuildEventArgs started = null;
                BuildEventArgs finished = null;
                foreach (var s in g)
                {
                    var f = finishedEventArgs[strategy.GetBuildEventId(s)];
                    var duration = f.Timestamp - s.Timestamp;
                    if (duration > maxDuration)
                    {
                        maxDuration = duration;
                        started = s;
                        finished = f;
                    }
                }

                m_msbuildNodes.EnsureIndex(started.BuildEventContext.NodeId);
                m_msbuildNodes[started.BuildEventContext.NodeId].Add(new()
                {
                    name = strategy.GetTraceEventName(started),
                    ph = "X",
                    ts = (started.Timestamp - firstObservedTime).TotalMicroseconds(),
                    dur = (finished.Timestamp - started.Timestamp).TotalMicroseconds(),
                    tid = started.BuildEventContext.NodeId,
                    pid = "1"
                });
            }

            for (int i = 0; i < m_msbuildNodes.Count; ++i)
            {
                if (m_msbuildNodes[i] == null)
                {
                    continue;
                }
                m_msbuildNodes[i].Sort((x,y) => x.ts.CompareTo(y.ts));
            }
        }

        public int MaxNodeId => m_msbuildNodes.Count - 1;

        public IEnumerable<TraceEvent> YieldEvents(int nodeId) => m_msbuildNodes[nodeId];
    }
}
