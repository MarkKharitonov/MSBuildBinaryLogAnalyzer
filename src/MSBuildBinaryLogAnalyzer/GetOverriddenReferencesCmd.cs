using System;
using ManyConsole;
using Newtonsoft.Json;

namespace MSBuildBinaryLogAnalyzer
{
    public partial class GetOverriddenReferencesCmd : ConsoleCommand
    {
        private string m_input;
        private bool m_json;

        public GetOverriddenReferencesCmd()
        {
            IsCommand("get-overridden-references", "Return all the references which are overridden by the system dependencies.");
            HasLongDescription(@"
This command scans the binary log for the following pattern:
>>>
Encountered conflict between 'Reference:.+' and 'Platform:.+'. Choosing 'Platform:.+' because AssemblyVersion '.+' is greater than '.+'
<<<
This means there is a dll reference (NuGet most likely) which is overridden by a
system dependency. We are interested in all occurrences of this condition, because
it breaks the Fast Up-To-Date Heuristic of the Visual Studio.

Returns an array of objects formatted as JSON.
");

            // Required options/flags, append '=' to obtain the required value.
            HasRequiredOption("i=", "A binary log file or directory of logs.", v => m_input = v);
            HasOption("json", "Format output as json.", _ => m_json = true);
        }

        public override int Run(string[] remainingArguments)
        {
            int exitCode;
            if ((exitCode = GetOverriddenReferences.VerifyParameters(m_input)) != 0)
            {
                return exitCode;
            }

            var res = GetOverriddenReferences.Run(m_input);
            if (m_json)
            {
                Console.WriteLine(JsonConvert.SerializeObject(res, Formatting.Indented));
            }
            else
            {
                res.ForEach(Console.WriteLine);
            }

            return 0;
        }
    }
}
