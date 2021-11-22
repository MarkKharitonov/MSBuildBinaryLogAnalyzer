using System;
using ManyConsole;

namespace MSBuildBinaryLogAnalyzer
{
    internal class Program
    {
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
                return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc);
                return 1;
            }
        }
    }
}