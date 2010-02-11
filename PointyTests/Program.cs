using System;
using System.Collections.Generic;
using System.Text;

//TODO - Build this into a (very) lightweight testing framework

namespace PointyTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- Pointy Tests ---");

            Console.WriteLine("- Running Powernap Tests -");
            Tests.ParserTests.TestParser(new Pointy.Parsers.Powernap() {MaximumEntitySize = 100000});
            Console.WriteLine("- Done -");
            Console.WriteLine();

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
