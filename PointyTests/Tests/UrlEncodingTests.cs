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

// Contains the test class
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