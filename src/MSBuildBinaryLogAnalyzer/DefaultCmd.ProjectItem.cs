using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MSBuildBinaryLogAnalyzer
{
    partial class DefaultCmd
    {
        [DebuggerDisplay("{ToString(), nq} ({m_projectId})")]
        public class ProjectItem
        {
            private readonly int m_projectId;
            private GenerateCompileDependencyCacheTarget m_generateCompileDependencyCacheTarget;
            private List<string> m_itemsToHash;

            public readonly string ProjectPath;
            public readonly string ProjectName;
            public readonly bool IsDesignTimeBuild;
            public bool IsTrigger { get; set; }
            public bool IsCompiled { get; set; }
            public List<(string FirstBuild, string SecondBuild)> Diff { get; set; }

            public ProjectItem(string projectPath, int projectId, bool isDesignTimeBuild = false)
            {
                ProjectPath = projectPath;
                m_projectId = projectId;
                ProjectName = Path.GetFileNameWithoutExtension(projectPath);
                IsDesignTimeBuild = isDesignTimeBuild;
            }

            public void DiffItemsToHash(List<string> itemsToHash) => Diff = CalcDiff(new List<string>(m_itemsToHash), itemsToHash);

            private static List<(string FirstBuild, string SecondBuild)> CalcDiff(List<string> thisItemsToHash, List<string> itemsToHash)
            {
                var diff = new List<(string FirstBuild, string SecondBuild)>();
                thisItemsToHash.RemoveAll(itemsToHash.Remove);

                var count = Math.Min(thisItemsToHash.Count, itemsToHash.Count);
                int i = 0;
                for (; i < count; ++i)
                {
                    if (thisItemsToHash[i] != itemsToHash[i])
                    {
                        int prevPos, pos = -1;
                        do
                        {
                            prevPos = pos;
                            pos = thisItemsToHash[i].IndexOf('\\', pos + 1);
                        } while (pos > 0 && string.Compare(thisItemsToHash[i], 0, itemsToHash[i], 0, pos) == 0);

                        diff.Add((FirstBuild: itemsToHash[i].Substring(prevPos), SecondBuild: thisItemsToHash[i].Substring(prevPos)));
                    }
                }
                for (; i < thisItemsToHash.Count; ++i)
                {
                    diff.Add((FirstBuild: null, SecondBuild: thisItemsToHash[i]));
                }
                for (; i < itemsToHash.Count; ++i)
                {
                    diff.Add((FirstBuild: itemsToHash[i], SecondBuild: null));
                }

                return diff;
            }

            public override string ToString()
            {
                var title = IsDesignTimeBuild ? $"{ProjectPath} (D)" : ProjectPath;

                if (Diff == null || Diff.Count == 0)
                {
                    return title;
                }

                return new StringBuilder(title)
                    .Append(" (")
                    .Append(string.Join(" , ", Diff.Select(d => $"...{d.FirstBuild} vs ...{d.SecondBuild}")))
                    .Append(")")
                    .ToString();
            }

            public bool AddIfGenerateCompileDependencyCacheTarget(Record ev)
            {
                if (ev.Args is TargetStartedEventArgs targetStarted && targetStarted.TargetName == "_GenerateCompileDependencyCache")
                {
                    Debug.Assert(ProjectPath == targetStarted.ProjectFile);
                    m_generateCompileDependencyCacheTarget = new GenerateCompileDependencyCacheTarget(targetStarted);
                }
                else if (m_generateCompileDependencyCacheTarget != null && !m_generateCompileDependencyCacheTarget.IsClosed)
                {
                    m_generateCompileDependencyCacheTarget.Children.Add(ev.Args);
                    return true;
                }
                return false;
            }

            public List<string> CalcItemsToHash() => m_itemsToHash = m_generateCompileDependencyCacheTarget.GetItemsToHash();
        }
    }
}