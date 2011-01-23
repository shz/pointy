// Program.cs
// Test launcher for Pointy

// Copyright (c) 2010 Patrick Stein
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;

namespace PointyTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Tests.PushTest("PointyUri");
            PointyUriTests.Run();
            Tests.PopTest();

            Tests.PushTest("UrlEncoding");
            UrlEncodingTests.Run();
            Tests.PopTest();

            Tests.PushTest("FormUrlencoded");
            FormUrlencodedTests.Run();
            Tests.PopTest();

            Tests.PushTest("Powernap");
            ParserTests.TestParser(new Pointy.Parsers.Powernap() {MaximumEntitySize = 1024 * 1024 * 10});
            Tests.PopTest();

            // UberStream is a prime candidate for testing, but I'm not really too worried about it
            // at this point.  Its functionality is essentially covered by the Powernap tests, given
            // the robustness of the entity testing.

            Tests.OutputConsole();

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}
