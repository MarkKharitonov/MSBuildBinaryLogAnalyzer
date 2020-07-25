using System.Diagnostics;
using System.IO;

namespace MSBuildBinaryLogAnalyzer
{
    partial class GetOverriddenReferences
    {
        [DebuggerDisplay("{ToString(), nq}")]
        public class OverriddenReferenceItem
        {
            public readonly string ProjectFilePath;
            public readonly string OverriddenReference;
            public readonly string SystemDependency;
            public readonly string SystemVersion;
            public readonly string OverriddenVersion;
            public readonly string ProjectName;

            public OverriddenReferenceItem(string projectFilePath, string overriddenReference, string systemDependency, string systemVersion, string overriddenVersion)
            {
                ProjectFilePath = projectFilePath;
                OverriddenReference = overriddenReference;
                SystemDependency = systemDependency;
                SystemVersion = systemVersion;
                OverriddenVersion = overriddenVersion;
                ProjectName = Path.GetFileNameWithoutExtension(projectFilePath);
            }

            public string ResolvedFilePath { get; internal set; }

            public override string ToString() => $"[{ProjectName}] {SystemDependency} {OverriddenVersion} -> {SystemVersion}";
        }
    }
}