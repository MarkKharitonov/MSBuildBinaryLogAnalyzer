using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ManyConsole;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging.StructuredLogger;

namespace MSBuildBinaryLogAnalyzer
{
    public partial class DefaultCmd : ConsoleCommand
    {
        private const string MARKER = @".csproj.CoreCompileInputs.cache"" is newer than output file ""obj\Debug\";
        private const string DESIGN_TIME_BUILD_MSG = @"Output file ""__NonExistentSubDir__\__NonExistentFile__"" does not exist.";
        private string m_input1;
        private string m_input2;

        public DefaultCmd()
        {
            IsCommand("default", "The default analysis.");
            HasLongDescription(@"
Checks if the given binary log contains any compilations ($task csc) due to newer compilation cache file.
Can be a directory of many binary logs for the same solution, which is useful for binary logs produced by
the Project System Tools extension for Visual Studio.

Can be given an additional binary log or directory, in which case it would compare the two compilation caches for the
respective project in both logs and output the difference.
");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("i=", "The first input binary log file or directory of logs.", v => m_input1 = v);
            HasOption("i2=", "The second input binary log file or directory of logs.", v => m_input2 = v);
        }

        public override int Run(string[] remainingArguments)
        {
            var binaryLogFilePath = m_input1;
            if (!File.Exists(binaryLogFilePath) && !Directory.Exists(binaryLogFilePath))
            {
                Console.Error.WriteLine($"File/Directory not found - {binaryLogFilePath}");
                return 1;
            }

            string binaryLogFilePath2 = null;
            if (m_input2 != null)
            {
                binaryLogFilePath2 = m_input2;
                if (!File.Exists(binaryLogFilePath2))
                {
                    Console.Error.WriteLine($"File/Directory not found - {binaryLogFilePath2}");
                    return 2;
                }
            }

            var projects = new Dictionary<int, ProjectItem>();
            EnumerateBinaryLogs(binaryLogFilePath).ForEach(file => ProcessFirstBinaryLog(file, binaryLogFilePath2, projects));

            if (projects.Values.Any(p => p.IsTrigger) && binaryLogFilePath2 != null)
            {
                EnumerateBinaryLogs(binaryLogFilePath2).ForEach(file => ProcessSecondBinaryLog(file, projects.Values));
            }

            if (Report("Triggers", projects.Values.Where(p => p.IsTrigger && !p.IsDesignTimeBuild)) + 
                Report("Recompiled projects", projects.Values.Where(p => p.IsCompiled && !p.IsDesignTimeBuild)) > 0)
            {
                return 3;
            }

            return 0;
        }

        private static IEnumerable<string> EnumerateBinaryLogs(string path) => Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.binlog")
            : (new[] { path });

        private static void ProcessFirstBinaryLog(string binaryLogFilePath, string binaryLogFilePath2, Dictionary<int, ProjectItem> projects)
        {
            var reader = new BinLogReader();
            foreach (var ev in reader.ReadRecords(binaryLogFilePath))
            {
                if (ev.Args?.BuildEventContext == null || ev.Args.BuildEventContext.ProjectInstanceId < 1)
                {
                    continue;
                }

                projects.AddIfProjectItem(ev);

                if (ev.Args is TaskStartedEventArgs taskStarted && taskStarted.TaskName == "Csc")
                {
                    var p = projects[taskStarted.BuildEventContext.ProjectInstanceId];
                    Debug.Assert(p.ProjectPath == taskStarted.ProjectFile);
                    p.IsCompiled = true;
                }

                if (binaryLogFilePath2 != null)
                {
                    projects.AddIfGenerateCompileDependencyCacheTarget(ev);
                }

                if (ev.Args is BuildMessageEventArgs msgEvent
                    && msgEvent.Message != null
                    && projects.TryGetValue(msgEvent.BuildEventContext.ProjectInstanceId, out var p2)
                    && !p2.IsTrigger
                    && (p2.IsDesignTimeBuild ? msgEvent.Message == DESIGN_TIME_BUILD_MSG : msgEvent.Message.Contains(MARKER)))
                {
                    if (binaryLogFilePath2 != null)
                    {
                        p2.CalcItemsToHash();
                    }

                    p2.IsTrigger = true;
                }
            }
        }

        private static void ProcessSecondBinaryLog(string binaryLogFilePath, ICollection<ProjectItem> projectRefs)
        {
            var reader = new BinLogReader();
            var projects = new Dictionary<int, ProjectItem>();
            foreach (var ev in reader.ReadRecords(binaryLogFilePath))
            {
                if (ev.Args?.BuildEventContext == null || ev.Args.BuildEventContext.ProjectInstanceId < 1)
                {
                    continue;
                }

                projects.AddIfProjectItem(ev);
                var p = projects.AddIfGenerateCompileDependencyCacheTarget(ev);
                if (p != null)
                {
                    ProjectItem trigger;
                    if ((trigger = projectRefs.GetMatchingTrigger(ev)) != null)
                    {
                        trigger.DiffItemsToHash(p.CalcItemsToHash());
                    }
                }
            }
        }

        private static int Report<T>(string category, IEnumerable<T> values) where T : class
        {
            var count = values.Count();
            if (count == 0)
            {
                return 0;
            }

            Console.WriteLine($"{count} {category}:");
            foreach (var v in values)
            {
                Console.WriteLine($"  {v}");
            }

            return 1;
        }
    }
}
