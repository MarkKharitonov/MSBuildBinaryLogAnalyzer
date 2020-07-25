using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Logging.StructuredLogger;

namespace MSBuildBinaryLogAnalyzer
{
    public static partial class GetOverriddenReferences
    {
        private const string ENCOUNTERED_CONFLICT_MARKER = "Choosing 'Platform:";
        private const string ENCOUNTERED_CONFLICT_PATTERN = @"^\s*Encountered conflict between 'Reference:(.+)' and 'Platform:.+'. Choosing 'Platform:(.+)' because AssemblyVersion '(.+)' is greater than '(.+)'\.$";
        private static readonly Regex s_encounteredConflictRegex = new Regex(ENCOUNTERED_CONFLICT_PATTERN);

        private const string RESOLVED_FILE_PATH_MARKER = "Resolved file path is";
        private const string RESOLVED_FILE_PATH_PATTERN = @"^\s*Resolved file path is ""(.+)"".$";
        private static readonly Regex s_resolvedFilePathRegex = new Regex(RESOLVED_FILE_PATH_PATTERN);

        public static int VerifyParameters(string binaryLogFilePath)
        {
            if (!File.Exists(binaryLogFilePath) && !Directory.Exists(binaryLogFilePath))
            {
                Console.Error.WriteLine($"File/Directory not found - {binaryLogFilePath}");
                return 1;
            }
            return 0;
        }

        public static IEnumerable<OverriddenReferenceItem> Run(string binaryLogFilePath) => binaryLogFilePath.EnumerateBinaryLogs().SelectMany(YieldOverriddenReferences);

        private static IEnumerable<OverriddenReferenceItem> YieldOverriddenReferences(string binaryLogFilePath)
        {
            var reader = new BinLogReader();
            var checkEncounteredConflictPattern = new NodeList<bool>();
            var checkResolvedFilePathPattern = new NodeList<bool>();
            var overriddenItems = new List<OverriddenReferenceItem>();
            foreach (var ev in reader.ReadRecords(binaryLogFilePath))
            {
                if (ev.Args?.BuildEventContext == null || ev.Args.BuildEventContext.ProjectInstanceId < 1)
                {
                    continue;
                }

                var nodeId = ev.Args.BuildEventContext.NodeId;
                if (checkEncounteredConflictPattern[nodeId])
                {
                    PopulateOverriddenItems(nodeId, ev, checkEncounteredConflictPattern, overriddenItems);
                }
                else if (checkResolvedFilePathPattern[nodeId])
                {
                    var item = SetResolvedFilePathInOverriddenItem(nodeId, ev, checkResolvedFilePathPattern, overriddenItems);
                    if (item != null)
                    {
                        yield return item;
                    }
                }
                if (ev.Args is TaskStartedEventArgs taskStarted)
                {
                    switch (taskStarted.TaskName)
                    {
                    case "ResolvePackageFileConflicts":
                        checkEncounteredConflictPattern[nodeId] = true;
                        break;
                    case "ResolveAssemblyReference":
                        checkResolvedFilePathPattern[nodeId] = true;
                        break;
                    }
                }
            }
            if (overriddenItems.Count > 0)
            {
                throw new Exception($"Unexpected error. The following references are unresolved: {string.Join("; ", overriddenItems)}");
            }
        }

        private static OverriddenReferenceItem SetResolvedFilePathInOverriddenItem(int nodeId, Record ev, NodeList<bool> checkResolvedFilePathPattern, List<OverriddenReferenceItem> overriddenItems)
        {
            if (ev.Args is TaskFinishedEventArgs taskFinished && taskFinished.TaskName == "ResolveAssemblyReference")
            {
                checkResolvedFilePathPattern[nodeId] = false;
            }
            else if (ev.Args is BuildMessageEventArgs msgEvent && msgEvent.Message?.Contains(RESOLVED_FILE_PATH_MARKER) == true)
            {
                var m = s_resolvedFilePathRegex.Match(msgEvent.Message);
                if (m.Success)
                {
                    var resolvedFilePath = m.Groups[1].Value;
                    var i = overriddenItems.FindIndex(item =>
                        item.ProjectFilePath == msgEvent.ProjectFile &&
                        resolvedFilePath.EndsWith(item.SystemDependency, StringComparison.OrdinalIgnoreCase) &&
                        resolvedFilePath[resolvedFilePath.Length - item.SystemDependency.Length - 1] == '\\'
                    );
                    if (i >= 0)
                    {
                        var found = overriddenItems[i];
                        found.ResolvedFilePath = resolvedFilePath;
                        overriddenItems.RemoveAt(i);
                        return found;
                    }
                }
            }
            return null;
        }

        private static void PopulateOverriddenItems(int nodeId, Record ev, NodeList<bool> checkEncounteredConflictPattern, List<OverriddenReferenceItem> overriddenItems)
        {
            if (ev.Args is TaskFinishedEventArgs taskFinished && taskFinished.TaskName == "ResolvePackageFileConflicts")
            {
                checkEncounteredConflictPattern[nodeId] = false;
            }
            else if (ev.Args is BuildMessageEventArgs msgEvent && msgEvent.Message?.Contains(ENCOUNTERED_CONFLICT_MARKER) == true)
            {
                var m = s_encounteredConflictRegex.Match(msgEvent.Message);
                if (m.Success)
                {
                    overriddenItems.Add(new OverriddenReferenceItem(msgEvent.ProjectFile, m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value));
                }
            }
        }
    }
}
