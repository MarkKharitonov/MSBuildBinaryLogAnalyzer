using System;
using System.IO;
using System.Linq;
using ManyConsole;
using Newtonsoft.Json;

namespace MSBuildBinaryLogAnalyzer
{
    public partial class DeleteOverriddenReferencesCmd : ConsoleCommand
    {
        private const string DEFAULT_ROOT = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\";
        private string m_input;
        private string m_root = DEFAULT_ROOT;
        private bool m_json;

        public DeleteOverriddenReferencesCmd()
        {
            IsCommand("del-overridden-references", "Deletes the references which are overridden by the system dependencies.");
            HasLongDescription(@"
This command internally invokes get-overridden-references and deletes the found references
from the respective packages.config and project files IF the resolved file path is under the
path given by the --root argument.

Outputs the same objects as get-overridden-references.
");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("i=", "A binary log file or directory of logs.", v => m_input = v);
            HasOption("root", "A reference would be deleted only if it is resolved to a file somewhere under this folder. The default value is " + m_root, v => m_root = v);
            HasOption("json", "Format output as json.", _ => m_json = true);
        }

        public override int Run(string[] remainingArguments)
        {
            int exitCode;
            if ((exitCode = GetOverriddenReferences.VerifyParameters(m_input)) != 0)
            {
                return exitCode;
            }

            if (m_root[m_root.Length - 1] != '\\')
            {
                m_root += "\\";
            }

            var res = GetOverriddenReferences.Run(m_input);
            foreach (var o in res)
            {
                if (!m_json)
                {
                    Console.WriteLine(o);
                }
                if (o.ResolvedFilePath.StartsWith(m_root))
                {
                    DeleteFromPackagesConfigFile(o);
                    DeleteFromProjectFile(o);
                }
            }

            if (m_json)
            {
                Console.WriteLine(JsonConvert.SerializeObject(res, Formatting.Indented));
            }
            return 0;
        }

        internal void DeleteFromPackagesConfigFile(GetOverriddenReferences.OverriddenReferenceItem o)
        {
            var packagesConfigFilePath = $"{o.ProjectFilePath}\\..\\packages.config";
            // Assume the package Id matches the name of the system dependency. Does not have to be the case in general for NuGet packages.
            var pattern = $"\"{o.SystemDependency.Substring(0, o.SystemDependency.Length - 4)}\"";
            var lines = File
                .ReadAllLines(packagesConfigFilePath)
                .Where(line => line.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) < 0)
                .ToArray();
            File.WriteAllLines(packagesConfigFilePath, lines);
        }

        internal void DeleteFromProjectFile(GetOverriddenReferences.OverriddenReferenceItem o)
        {
            var skip = false;
            var lines = File
                .ReadAllLines(o.ProjectFilePath)
                .Where(line =>
                {
                    var skipNow = skip;
                    if (skip)
                    {
                        skip = line.IndexOf("</Reference>", StringComparison.OrdinalIgnoreCase) < 0;
                    }
                    else
                    {
                        skipNow = skip = line.IndexOf($"Reference Include=\"{o.OverriddenReference}\"", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    return !skipNow;
                })
                .ToArray();
            File.WriteAllLines(o.ProjectFilePath, lines);
        }
    }
}
