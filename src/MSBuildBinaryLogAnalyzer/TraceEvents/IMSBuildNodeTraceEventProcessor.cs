using System.Collections.Generic;

namespace MSBuildBinaryLogAnalyzer.TraceEvents
{
    public interface IMSBuildNodeTraceEventProcessor
    {
        IEnumerable<TraceEvent> YieldEvents(int nodeId);
    }
}
