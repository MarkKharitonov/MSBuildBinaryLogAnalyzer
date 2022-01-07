using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace MSBuildBinaryLogAnalyzer
{
    public static class Extensions
    {
        public static IEnumerable<string> EnumerateBinaryLogs(this string path) => Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*.binlog")
            : (new[] { path });

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
            {
                action(item);
            }
        }

        public static uint TotalMicroseconds(this TimeSpan ts) => (uint)(ts.TotalMilliseconds * 1_000);
    
        public static (int, int) TargetId(this BuildEventContext o) => (o.NodeId, o.TargetId);
        public static (int, int) TargetId(this BuildEventArgs o) => o.BuildEventContext.TargetId();
        public static string ProjectName(this ProjectStartedEventArgs o) => Path.GetFileName(o.ProjectFile);
        public static string ProjectName(this TargetStartedEventArgs o) => Path.GetFileName(o.ProjectFile);
        public static bool IsActualProjectBuildEvent(this ProjectStartedEventArgs o) => (o.TargetNames == "" || o.TargetNames == "Build") && !o.ProjectFile.EndsWith(".metaproj");
        public static void EnsureIndex<T>(this IList<T> items, int index) where T : new()
        {
            while (index >= items.Count)
            {
                items.Add(new T());
            }
        }
    }
}
