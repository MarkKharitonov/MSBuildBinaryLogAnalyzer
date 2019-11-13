using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using static MSBuildBinaryLogAnalyzer.DefaultCmd;

namespace MSBuildBinaryLogAnalyzer
{
    public static class DefaultCmd_Extensions
    {
        public static void AddIfProjectItem(this Dictionary<int, ProjectItem> projects, Record ev)
        {
            if (ev.Args is ProjectStartedEventArgs projectStarted)
            {
                if (projects.TryGetValue(projectStarted.ProjectId, out var p))
                {
                    Debug.Assert(p.ProjectPath == projectStarted.ProjectFile);
                }
                else
                {
                    var isDesignTimeBuild = projectStarted.GlobalProperties != null &&
                        projectStarted.GlobalProperties.TryGetValue("DesignTimeBuild", out var designTimeBuild) &&
                        bool.TrueString.Equals(designTimeBuild, StringComparison.OrdinalIgnoreCase);
                    projects[projectStarted.ProjectId] = new ProjectItem(projectStarted.ProjectFile, projectStarted.ProjectId, isDesignTimeBuild);
                }
            }
        }

        public static ProjectItem AddIfGenerateCompileDependencyCacheTarget(this Dictionary<int, ProjectItem> projects, Record ev)
        {
            var p = projects[ev.Args.BuildEventContext.ProjectInstanceId];
            return p.AddIfGenerateCompileDependencyCacheTarget(ev) ? p : null;
        }

        public static ProjectItem GetMatchingTrigger(this IEnumerable<ProjectItem> projects, Record ev)
        {
            if (ev.Args is BuildMessageEventArgs msgEvent)
            {
                var msg = msgEvent.Message;
                return projects.FirstOrDefault(t =>
                    t.IsTrigger &&
                    msg.StartsWith($"Added Item(s): FileWrites=obj\\Debug\\") &&
                    msg.EndsWith($"\\{t.ProjectName}.csproj.CoreCompileInputs.cache"));
            }
            return null;
        }
    }
}