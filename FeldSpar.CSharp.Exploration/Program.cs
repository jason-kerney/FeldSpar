using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeldSpar.ClrInterop;
using FeldSpar.Framework;
using Microsoft.FSharp.Core;

namespace FeldSpar.CSharp.Exploration
{
    class Program
    {
        static void Main(string[] args)
        {
            var eng = new FeldSpar.ClrInterop.Engine();
            eng.TestFound += eng_TestFound;
            eng.TestRunning += eng_TestRunning;
            eng.TestFinished += eng_TestFinnished;
            eng.FindTests(@"C:\Users\Jason\Documents\GitHub\FeldSpar\FeldSpar.CSharp.Exploration\bin\Debug\FeldSpar.Tests.dll");
            eng.RunTests(@"C:\Users\Jason\Documents\GitHub\FeldSpar\FeldSpar.CSharp.Exploration\bin\Debug\FeldSpar.Tests.dll");

            System.Console.WriteLine("Done");
            System.Console.ReadKey(true);
        }

        private static void eng_TestFinnished(object sender, TestCompeteEventArgs args)
        {
            ConsoleColor color;

            if (args.TestResult.IsFailure)
            {
                color = System.ConsoleColor.Red;
            }
            else
            {
                color = System.ConsoleColor.Green;
            }

            WriteMessage(color, args.Name);
        }

        static void eng_TestRunning(object sender, ClrInterop.TestEventArgs args)
        {
            WriteMessage(ConsoleColor.Blue, args.Name);
        }

        static void eng_TestFound(object sender, ClrInterop.TestEventArgs args)
        {
            WriteMessage(ConsoleColor.Gray, args.Name);
        }

        private static void WriteMessage(ConsoleColor consoleColor, string message)
        {
            using (new ConsoleColorer(consoleColor))
            {
                System.Console.WriteLine(message);
            }
        }
    }

    public class ConsoleColorer : IDisposable
    {
        private readonly ConsoleColor color;

        public ConsoleColorer(ConsoleColor color)
        {
            this.color = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            System.Console.ForegroundColor = color;
        }
    }
}
