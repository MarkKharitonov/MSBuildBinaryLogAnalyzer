using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging.StructuredLogger;

namespace MSBuildBinaryLogAnalyzer
{
    internal class Program
    {
        private const string MARKER = @".csproj.CoreCompileInputs.cache"" is newer than output file ""obj\Debug\";
        private const string PATTERN = @"Input file ""obj\\Debug\\(.+)\.csproj\.CoreCompileInputs\.cache"" is newer than output file ""obj\\Debug\\.+""\.";
        private static readonly Regex s_regex = new Regex(PATTERN);

        public static int Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                Console.Error.WriteLine($"Usage: {Path.GetFileName(Assembly.GetEntryAssembly().Location)} binary_log_file_path [another_binary_log_file_path]");
                return 1;
            }

            var binaryLogFilePath = args[0];
            if (!File.Exists(binaryLogFilePath))
            {
                Console.Error.WriteLine($"File not found - {binaryLogFilePath}");
                return 2;
            }

            string binaryLogFilePath2 = null;
            if (args.Length == 2)
            {
                binaryLogFilePath2 = args[1];
                if (!File.Exists(binaryLogFilePath2))
                {
                    Console.Error.WriteLine($"File not found - {binaryLogFilePath2}");
                    return 2;
                }
            }

            var triggers = new List<Trigger>();
            var projects = new List<string>();
            var reader = new BinLogReader();
            var generateCompileDependencyCacheTargets = new NodeList<GenerateCompileDependencyCacheTarget>();
            foreach (var ev in reader.ReadRecords(binaryLogFilePath))
            {
                if (ev.Args?.BuildEventContext == null)
                {
                    continue;
                }

                if (ev.Args is TaskStartedEventArgs taskStarted && taskStarted.TaskName == "Csc")
                {
                    projects.Add(taskStarted.ProjectFile);
                }

                int nodeId = Math.Max(0, ev.Args.BuildEventContext.NodeId);
                if (binaryLogFilePath2 != null)
                {
                    if (ev.Args is TargetStartedEventArgs targetStarted && targetStarted.TargetName == "_GenerateCompileDependencyCache")
                    {
                        generateCompileDependencyCacheTargets[nodeId] = new GenerateCompileDependencyCacheTarget(targetStarted);
                    }
                    else if (generateCompileDependencyCacheTargets[nodeId] != null && !generateCompileDependencyCacheTargets[nodeId].IsClosed)
                    {
                        generateCompileDependencyCacheTargets[nodeId].Children.Add(ev.Args);
                    }
                }

                Match m;
                if (ev.Args is BuildMessageEventArgs msgEvent
                    && msgEvent.Message != null
                    && msgEvent.Message.Contains(MARKER)
                    && (m = s_regex.Match(msgEvent.Message)).Success)
                {
                    List<string> items = null;
                    if (binaryLogFilePath2 != null)
                    {
                        items = generateCompileDependencyCacheTargets[nodeId].GetItemsToHash();
                    }

                    triggers.Add(new Trigger(m.Groups[1].Value, items));
                }
            }

            if (triggers.Count > 0 && binaryLogFilePath2 != null)
            {
                ProcessSecondBinaryLog(binaryLogFilePath2, triggers);
            }

            if (Report("Triggers", triggers) + Report("Recompiled projects", projects) > 0)
            {
                return 3;
            }

            return 0;
        }

        private static void ProcessSecondBinaryLog(string binaryLogFilePath, List<Trigger> triggers)
        {
            var reader = new BinLogReader();
            var generateCompileDependencyCacheTargets = new NodeList<GenerateCompileDependencyCacheTarget>();
            foreach (var ev in reader.ReadRecords(binaryLogFilePath))
            {
                if (ev.Args?.BuildEventContext == null)
                {
                    continue;
                }

                int nodeId = Math.Max(0, ev.Args.BuildEventContext.NodeId);
                if (ev.Args is TargetStartedEventArgs targetStarted && targetStarted.TargetName == "_GenerateCompileDependencyCache")
                {
                    generateCompileDependencyCacheTargets[nodeId] = new GenerateCompileDependencyCacheTarget(targetStarted);
                }
                else if (generateCompileDependencyCacheTargets[nodeId] != null && !generateCompileDependencyCacheTargets[nodeId].IsClosed)
                {
                    Trigger trigger;
                    if (ev.Args is BuildMessageEventArgs msgEvent && (trigger = GetMatchingTrigger(triggers, msgEvent.Message)) != null)
                    {
                        trigger.DiffItemsToHash(generateCompileDependencyCacheTargets[nodeId].GetItemsToHash());
                        generateCompileDependencyCacheTargets[nodeId] = null;
                    }
                    else
                    {
                        generateCompileDependencyCacheTargets[nodeId].Children.Add(ev.Args);
                    }
                }
            }
        }

        private static Trigger GetMatchingTrigger(IEnumerable<Trigger> triggers, string msg) => 
            triggers.FirstOrDefault(t => msg == $"Added Item(s): FileWrites=obj\\Debug\\{t.ProjectName}.csproj.CoreCompileInputs.cache");

        private static int Report<T>(string category, IReadOnlyCollection<T> values) where T : class
        {
            if (values.Count <= 0)
            {
                return 0;
            }

            Console.WriteLine($"{category}:");
            foreach (var v in values)
            {
                Console.WriteLine($"  {v}");
            }

            return 1;
        }
    }
}