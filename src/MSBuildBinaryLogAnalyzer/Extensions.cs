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
    }
}
