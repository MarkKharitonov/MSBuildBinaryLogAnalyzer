using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManyConsole;
using Microsoft.Build.Logging.StructuredLogger;
using MSBuildBinaryLogAnalyzer.TraceEvents;
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
            var events = YieldEvents(input, target);

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
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
            serializer.Serialize(file, events);
        }

        private static IEnumerable<TraceEvent> YieldEvents(string input, string target)
        {
            IBuildEventArgsStrategy strategy = target == null ? new ProjectEventArgsStrategy() : new TargetEventArgsStrategy(target);
            var argsList = BinaryLog.ReadRecords(input).Select(o => o.Args).ToList();
            var initialProcessor = new InitialTraceEventProducer(argsList, strategy);
            var advancedProcessor = new TraceEventCompressor(initialProcessor);
            return Enumerable.Range(1, initialProcessor.MaxNodeId).SelectMany(advancedProcessor.YieldEvents);
        }
    }
}
