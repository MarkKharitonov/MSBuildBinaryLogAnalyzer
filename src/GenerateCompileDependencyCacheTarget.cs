using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace MSBuildBinaryLogAnalyzer
{
    internal class GenerateCompileDependencyCacheTarget
    {
        public readonly TargetStartedEventArgs TargetStarted;
        public readonly List<BuildEventArgs> Children = new List<BuildEventArgs>();

        public GenerateCompileDependencyCacheTarget(TargetStartedEventArgs targetStarted)
        {
            TargetStarted = targetStarted;
        }

        public bool IsClosed => Children.Count > 0
                                && Children.Last() is TargetFinishedEventArgs targetFinished
                                && targetFinished.BuildEventContext.TargetId == TargetStarted.BuildEventContext.TargetId;

        public List<string> GetItemsToHash() =>
            Children
                .OfType<BuildMessageEventArgs>()
                .FirstOrDefault(o => o.Message.Contains("ItemsToHash="))?.Message
                .Split('\n')
                .Where(s => s.StartsWith("        ") && s[8] != ' ')
                .Select(s => s.Trim())
                .ToList();
    }
}