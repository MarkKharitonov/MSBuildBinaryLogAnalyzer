using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManyConsole;
using Microsoft.Build.Logging.StructuredLogger;
using Newtonsoft.Json;

namespace MSBuildBinaryLogAnalyzer
{
    public partial class GetTraceEventsCmd : ConsoleCommand
    {
        private string m_input;
        private string m_target;
        private string m_output;

        public GetTraceEventsCmd()
        {
            IsCommand("get-trace-events", "Gets trace events for the projects or the given target across all the projects.");

            HasRequiredOption("i|input=", "A binary log file.", v => m_input = v);
            HasOption("o|output=", "The output directory", v => m_output = v);
            HasOption("t|target=", "The target to focus on. By default the focus is on the entire project.", v => m_target = v);
        }

        public override int Run(string[] remainingArguments)
        {
            Run(m_input, m_target, m_output);
            return 0;
        }

        internal static void Run(string input, string target, string output)
        {
            var build = BinaryLog.ReadBuild(input);
            BuildAnalyzer.AnalyzeBuild(build);
            var events = YieldEvents(build, target);

            var fileNameSuffix = target == null ? "_events.json" : $"_events_for_{target}.json";
            if (output != null)
            {
                Directory.CreateDirectory(output);
                output = Path.Combine(output, Path.GetFileName(input).Replace(".binlog", fileNameSuffix));
            }
            else
            {
                output = input.Replace(".binlog", fileNameSuffix);
            }
            using var file = File.CreateText(output);
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            serializer.Serialize(file, events);
        }

        private static IEnumerable<TraceEvent> YieldEvents(Build build, string targetName)
        {
            IReadOnlyCollection<TimedNode> foundNodes;
            Func<TimedNode, string> getName;
            if (targetName != null)
            {
                foundNodes = build.FindChildrenRecursive<Target>(o => o.Name == targetName);
                getName = n => ((Target)n).Project.Name;
            }
            else
            {
                foundNodes = build.FindChildrenRecursive<Project>(o =>
                    !o.Name.EndsWith(".metaproj") &&
                    (o.EntryTargets.Count == 0 || o.EntryTargets[0] == "Build") &&
                    !o.Children.OfType<Target>().Any(o => o.FirstChild is Message m && m.LookupKey == "Target \"Build\" skipped. Previously built successfully."));
                getName = n => n.Name;
            }

            foreach (var foundNode in foundNodes)
            {
                var name = getName(foundNode);
                yield return StartTraceEvent(foundNode, name, build.StartTime);
                yield return EndTraceEvent(foundNode, name, build.StartTime);
            }
        }

        private static TraceEvent EndTraceEvent(TimedNode node, string name, DateTime firstObservedTime) => new()
        {
            name = name,
            ph = "E",
            ts = (node.EndTime - firstObservedTime).TotalMicroseconds(),
            tid = node.NodeId,
            id = node.Id.ToString(),
            pid = "0"
        };

        private static TraceEvent StartTraceEvent(TimedNode node, string name, DateTime firstObservedTime) => new()
        {
            name = name,
            ph = "B",
            ts = (node.StartTime - firstObservedTime).TotalMicroseconds(),
            tid = node.NodeId,
            id = node.Id.ToString(),
            pid = "0"
        };
    }
}
