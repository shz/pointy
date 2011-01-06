﻿// Tests/UrlEncodingTests.cs
// Tests for Pointy's Util.UrlEncoding class

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
using PointyTests.UrlEncodingTest;

namespace PointyTests
{
    public class UrlEncodingTests
    {
        /// <summary>
        /// Tests UrlEncoding.Encode
        /// </summary>
        static Test[] EncodingTests = new Test[]
        {
            new Test("No Escaping", "abc123", "abc123"),
            new Test("Basic Escaping", "foobar-$$%", "foobar-%24%24%25"),
            new Test("Control Character Escaping", "\x02\x03\x04", "%02%03%04"),
            new Test("Extended ASCII Escaping", "bb\xE4\xF1", "bb%e4%f1"),
        };

        /// <summary>
        /// Tests UrlEncoding.Decode
        /// </summary>
        static Test[] DecodingTests = new Test[]
        {
            new Test("No Escaping", "abc123", "abc123"),
            new Test("Basic Escaping", "foobar-%24%24%25", "foobar-$$%"),
            new Test("Control Character Escaping", "%02%03%04", "\x02\x03\x04"),
            new Test("Extended ASCII Escaping", "bb%e4%f1", "bb\xE4\xF1"),
        };

        public static void Run()
        {
            Tests.PushTest("Encoding Tests");
            for (int i = 0; i < EncodingTests.Length; i++)
            {
                Tests.PushTest(EncodingTests[i].Name);
                Tests.Expect(EncodingTests[i].Output, UrlEncoding.Encode(EncodingTests[i].Input));
                Tests.PopTest();
            }
            Tests.PopTest();

            Tests.PushTest("Decoding Tests");
            for (int i = 0; i < DecodingTests.Length; i++)
            {
                Tests.PushTest(DecodingTests[i].Name);
                Tests.Expect(DecodingTests[i].Output, UrlEncoding.Decode(DecodingTests[i].Input));
                Tests.PopTest();
            }
            Tests.PopTest();
        }
    }
}

// Contains the Test class
namespace PointyTests.UrlEncodingTest
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
        public string Output
        {
            get;
            set;
        }

        public Test(string name, string input, string output)
        {
            Name = name;
            Input = input;
            Output = output;
        }
    }
}