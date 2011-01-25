// Tests/FormUrlencodedTests.cs
// Tests for Pointy's Util.MimeType class

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
using PointyTests.MimeTypeTest;

namespace PointyTests
{
    class MimeTypeTests
    {
        static Test[] MimeTests = new Test[]
        {
            new Test("Basic", "text", "html", "text/html"),
            new Test("Basic with one parameter", "text", "plain", new Dictionary<string, string>()
                {
                    {"charset", "UTF-8"}
                },
            "text/plain; charset=UTF-8")
        };

        public static void Run()
        {
            //parsing
            Tests.PushTest("MimeType Generation");
            foreach (var test in MimeTests)
            {
                Tests.PushTest(test.Name);

                MimeType m = new MimeType(test.Type, test.Subtype, test.Parameters);
                Tests.Expect(test.String, m.ToString(), "Output is correct");

                Tests.PopTest();
            }
            Tests.PopTest();

            //generation
            Tests.PushTest("MimeType Parsing");
            foreach (var test in MimeTests)
            {
                Tests.PushTest(test.Name);

                MimeType m = MimeType.Parse(test.String);
                Tests.Expect(test.Type, m.Type, "Type is correct");
                Tests.Expect(test.Subtype, m.Subtype, "Subtype is correct");
                Tests.DictCompare<string, string>(test.Parameters, m.Parameters as Dictionary<string, string>);

                Tests.PopTest();
            }
            Tests.PopTest();
        }
    }
}

namespace PointyTests.MimeTypeTest
{
    class Test
    {
        public string Name
        {
            get;
            set;
        }
        public string Type
        {
            get;
            set;
        }
        public string Subtype
        {
            get;
            set;
        }
        public Dictionary<string, string> Parameters
        {
            get;
            set;
        }
        public string String
        {
            get;
            set;
        }

        public Test(string name, string type, string subtype, string str) : this(name, type, subtype, new Dictionary<string, string>(), str)
        {
            
        }
        public Test(string name, string type, string subtype, Dictionary<string, string> parameters, string str)
        {
            Name = name;
            Type = type;
            Subtype = subtype;
            Parameters = parameters;
            String = str;
        }
    }
}
