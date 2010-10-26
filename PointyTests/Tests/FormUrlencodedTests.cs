// Tests/FormUrlencodedTests.cs
// Tests for Pointy's Util.FormUrlencoded class

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

using Pointy.Util;
using PointyTests.FormUrlencodingTest;

namespace PointyTests
{
    class FormUrlencodedTests
    {
        /// <summary>
        /// Tests FormUrlencoded.Parse
        /// </summary>
        static Test[] ParsingTests = new Test[]
        {
            new Test("Single entry", "foo=bar", new Dictionary<string,string>()
            {
                {"foo", "bar"}
            }),
            new Test("Multiple entries", "foo=bar&baz=bam", new Dictionary<string,string>()
            {
                {"foo", "bar"},
                {"baz", "bam"}
            }),
            new Test("Escaping", "foo%3D=bar", new Dictionary<string,string>()
            {
                {"foo=", "bar"}
            })
        };


        public static void Run()
        {
            Tests.PushTest("Parsing Tests");
            for (int i = 0; i < ParsingTests.Length; i++)
            {
                Tests.PushTest(ParsingTests[i].Name);
                Tests.DictCompare<string, string>(ParsingTests[i].Output, FormUrlencoded.Parse(ParsingTests[i].Input));
                Tests.PopTest();
            }
            Tests.PopTest();
        }
    }
}

// Contains the Test class
namespace PointyTests.FormUrlencodingTest
{
    class Test
    {
        public string Name
        {
            get;
            set;
        }
        public string Input
        {
            get;
            set;
        }
        public Dictionary<string, string> Output
        {
            get;
            set;
        }

        public Test(string name, string input, Dictionary<string, string> output)
        {
            Name = name;
            Input = input;
            Output = output;
        }
    }
}
