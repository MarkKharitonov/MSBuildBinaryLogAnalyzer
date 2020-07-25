using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ManyConsole;

namespace MSBuildBinaryLogAnalyzer
{
    internal class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetConsoleProcessList(int[] pids, int arraySize);

        private static bool OwnsConsole()
        {
            var pids = new int[1];   // Intentionally too short
            return GetConsoleProcessList(pids, pids.Length) == 1;
        }

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                var commands = ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
                foreach (var c in commands)
                {
                    c.SkipsCommandSummaryBeforeRunning();
                }
                args = SupportCompatibilityMode(args, commands);
                return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc);
                return 1;
            }
            finally
            {
                if (!Console.IsOutputRedirected && OwnsConsole())
                {
                    Console.WriteLine("Press any key to exit ...");
                    Console.ReadKey();
                }
            }
        }

        private static string[] SupportCompatibilityMode(string[] args, IEnumerable<ConsoleCommand> commands)
        {
            if ((args.Length == 1 || args.Length == 2) && commands.All(c => !string.Equals(c.Command, args[0], StringComparison.OrdinalIgnoreCase)))
            {
                // Compatibility mode.
                var a = new string[1 + 2 * args.Length];
                a[0] = "default";
                a[1] = "-i";
                a[2] = args[0];
                if (args.Length == 2)
                {
                    a[3] = "--i2";
                    a[4] = args[1];
                }
                args = a;
            }

            return args;
        }
    }
}