using System;
using System.Collections.Generic;

namespace MSBuildBinaryLogAnalyzer.TraceEvents
{
    public class TraceEventCompressor : IMSBuildNodeTraceEventProcessor
    {
        private readonly IMSBuildNodeTraceEventProcessor m_input;

        public TraceEventCompressor(IMSBuildNodeTraceEventProcessor input)
        {
            m_input = input;
        }

        public IEnumerable<TraceEvent> YieldEvents(int nodeId)
        {
            var it = m_input.YieldEvents(nodeId).GetEnumerator();
            if (!it.MoveNext())
            {
                yield break;
            }
            var partials = new List<TraceEvent>();
            var prev = it.Current;
            while (prev != null)
            {
                TraceEvent next = null;
                uint tsNext = uint.MaxValue;
                if (it.MoveNext())
                {
                    next = it.Current;
                    tsNext = next.ts;
                }

                if (prev.ts > tsNext)
                {
                    throw new ArgumentException();
                }

                if (prev.end <= tsNext)
                {
                    yield return prev;
                    foreach (var partial in YieldPartials(partials, prev, tsNext))
                    {
                        yield return partial;
                    }
                }
                else if (prev.ts < tsNext)
                {
                    yield return NewPartialTraceEvent(prev, prev.ts, tsNext);
                    AddPartial(partials, prev);
                }
                prev = next;
            }

            static void AddPartial(List<TraceEvent> partials, TraceEvent prev)
            {
                int i;
                for (i = 0; i < partials.Count && (partials[i].end < prev.end || partials[i].end == prev.end && partials[i].ts >= prev.ts); ++i)
                {
                }
                partials.Insert(i, prev);
            }

            static IEnumerable<TraceEvent> YieldPartials(List<TraceEvent> partials, TraceEvent prev, uint tsNext)
            {
                var ts = prev.end;
                while (partials.Count > 0 && partials[0].end <= ts)
                {
                    yield return NewPartialTraceEvent(partials[0], partials[0].end, partials[0].end, "i");
                    partials.RemoveAt(0);
                }

                while (partials.Count > 0)
                {
                    var cur = partials[0];
                    uint end = tsNext;
                    if (cur.end <= tsNext)
                    {
                        end = cur.end;
                        partials.RemoveAt(0);
                    }

                    if (end > ts)
                    {
                        yield return NewPartialTraceEvent(cur, ts, end);
                    }
                    else if (end == cur.end)
                    {
                        yield return NewPartialTraceEvent(cur, ts, end, "i");
                    }

                    if (cur.end > tsNext)
                    {
                        break;
                    }

                    ts = cur.end;
                }
            }

            static TraceEvent NewPartialTraceEvent(TraceEvent whole, uint ts, uint end, string phase = null) => new()
            {
                name = "|" + whole.name,
                ph = phase ?? whole.ph,
                ts = ts,
                dur = end - ts,
                tid = whole.tid,
                pid = whole.pid,
                args = new()
                {
                    ["0.percents"] = $"{(end - whole.ts) * 100.0 / whole.dur:N2}%",
                    ["1.start"] = TraceEvent.FormatTimestamp(whole.ts),
                    ["2.duration"] = TraceEvent.FormatTimestamp(whole.dur),
                    ["3.end"] = TraceEvent.FormatTimestamp(whole.ts + whole.dur),
                }
            };
        }
    }
}
