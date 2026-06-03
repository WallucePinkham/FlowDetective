using System;
using System.IO;
using System.Linq;

namespace FlowDetective
{
    internal class Program
    {   
        static void Main(string[] args)
        {
            // Delegate to Cli.Run so CLI logic is testable.
            Cli.Run(args, Console.Out);
        }
    }
}
