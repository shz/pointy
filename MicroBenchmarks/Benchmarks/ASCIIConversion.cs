using System;
using System.Collections.Generic;
using System.Text;

namespace MicroBenchmarks.Benchmarks
{
    /// <summary>
    /// Serious business, this is.
    /// </summary>
    class ASCIIConversion
    {
        #region ASCII Decoder Array

        static readonly char[] ASCII = new char[]
        {
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06',
            '\a', '\b', '\t', '\n', '\v', '\f', '\r', '\x0E', '\x0F', 
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', 
            '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', 
            '\x1E', '\x1F', ' ', '!', '"', '#', '$', '%', '&', '\'',
            '(', ')', '*', '+', '\x2C', '-', '.', '/', '0', '1', '2',
            '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>',
            '?', '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
            'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b',
            'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '{', '|', '}', '~', '\x7F'
        };

        #endregion

        //Test options
        static int? Seed = null; //null for no seed
        const int Runs = 10000000; //10 million

        public static void Run()
        {
            //A benchmark's best friends
            long start, finish;
            Random r = Seed == null ? new Random() : new Random(Seed.Value);
            byte[] buffer = new byte[Runs];
            char c;

            //test array method first
            Console.WriteLine("Converting {0} random ASCII characters using an array...", Runs);
            r.NextBytes(buffer);
            start = System.Environment.TickCount;
            for (int i = 0; i < Runs; i++)
                c = ASCII[buffer[i] / 2]; //clamp that down within ASCII range
            finish = System.Environment.TickCount;
            Console.WriteLine("Elapsed time: {0}ms", finish - start);

            //test GetChars method
            Console.WriteLine("Converting {0} random ASCII characters using GetChars()...", Runs);
            r.NextBytes(buffer);
            start = System.Environment.TickCount;
            for (int i = 0; i < Runs; i++)
            {
                char[] chars = Encoding.ASCII.GetChars(buffer, i, 1);
            }
            finish = System.Environment.TickCount;
            Console.WriteLine("Elapsed time: {0}ms", finish - start);

            //test GetChars method, again
            Console.WriteLine("Converting {0} random ASCII characters using GetChars() - one shot...", Runs);
            r.NextBytes(buffer);
            start = System.Environment.TickCount;
            char[] chars2 = Encoding.ASCII.GetChars(buffer);
            finish = System.Environment.TickCount;
            Console.WriteLine("Elapsed time: {0}ms", finish - start);

            //test Convert.ToChar method
            Console.WriteLine("Converting {0} random ASCII characters using Convert.ToChar()...", Runs);
            r.NextBytes(buffer);
            start = System.Environment.TickCount;
            for (int i = 0; i < Runs; i++)
                c = Convert.ToChar(buffer[i]);
            finish = System.Environment.TickCount;
            Console.WriteLine("Elapsed time: {0}ms", finish - start);

            //test casting method, which is what Convert does behind the scenes
            Console.WriteLine("Converting {0} random ASCII characters using casting...", Runs);
            start = System.Environment.TickCount;
            for (int i = 0; i < Runs; i++)
                c = (char)(buffer[i]); //clamp that down within ASCII range
            finish = System.Environment.TickCount;
            Console.WriteLine("Elapsed time: {0}ms", finish - start);

            // Conclusion: Straight up casting is the fastest and safest method.

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}
