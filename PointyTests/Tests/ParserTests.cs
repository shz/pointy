// Tests/ParserTests.cs
// Generic parser tests for Pointy

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
using System.IO;

using Pointy;
using Pointy.HTTP;

//Tests to Add:
// - Safari
// - IE
// - Tests that should fail

namespace PointyTests
{
    /// <summary>
    /// Helper class for testing parsers
    /// </summary>
    class ParserTest
    {
        public string Name
        {
            get;
            set;
        }
        public byte[] Bytes
        {
            get;
            set;
        }
        public ParseResult Result
        {
            get;
            set;
        }

        public ParserTest(string name, byte[] bytes, ParseResult result)
        {
            Name = name;
            Bytes = bytes;
            Result = result;
        }


        static byte[] ASCII(params string[] strs)
        {
            return System.Text.Encoding.ASCII.GetBytes(String.Join(String.Empty, strs));
        }
        static string CharRepeat(char c, int n)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < n; i++)
                b.Append(c);
            return b.ToString();
        }

        /// <summary>
        /// Holds our test cases.
        /// </summary>
        public static readonly List<ParserTest> Tests = new List<ParserTest>()
        {
            //Tests ported from ryah's HTTP parser (http://github.com/ry/http-parser/blob/master/test.c)

            #region GET Curl
            {
                new ParserTest(
                    "Curl GET",
                    ASCII(
                        "GET /test HTTP/1.1\r\n",
                        "User-Agent: curl/7.18.0 (i486-pc-linux-gnu) libcurl/7.18.0 OpenSSL/0.9.8g zlib/1.2.3.3 libidn/1.1\r\n",
                        "Host: 0.0.0.0=5000\r\n",
                        "Accept: */*\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/test", new Dictionary<string,string>()
                    {
                        {"User-Agent", "curl/7.18.0 (i486-pc-linux-gnu) libcurl/7.18.0 OpenSSL/0.9.8g zlib/1.2.3.3 libidn/1.1"},
                        {"Host", "0.0.0.0=5000"},
                        {"Accept", "*/*"}
                    }, null), false)
                )
            },
            #endregion
            #region GET Firefox
            {
                new ParserTest(
                    "GET Firefox",
                    ASCII(
                        "GET /favicon.ico HTTP/1.1\r\n",
                        "User-Agent: Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9) Gecko/2008061015 Firefox/3.0\r\n",
                        "Host: 0.0.0.0=5000\r\n",
                        "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n",
                        "Accept-Language: en-us,en;q=0.5\r\n",
                        "Accept-Encoding: gzip,deflate\r\n",
                        "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7\r\n",
                        "Keep-Alive: 300\r\n",
                        "Connection: keep-alive\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/favicon.ico", new Dictionary<string,string>()
                    {
                        {"User-Agent", "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9) Gecko/2008061015 Firefox/3.0"},
                        {"Host", "0.0.0.0=5000"},
                        {"Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"},
                        {"Accept-Language", "en-us,en;q=0.5"},
                        {"Accept-Encoding", "gzip,deflate"},
                        {"Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7"},
                        {"Keep-Alive", "300"},
                        {"Connection", "keep-alive"}
                    }, null), false)
                )
            },
            #endregion
            #region GET Chrome
            {
                new ParserTest(
                    "GET Chrome",
                    ASCII(
                        "GET / HTTP/1.1\r\n",
                        "Host: localhost:8000\r\n",
                        "Connection: keep-alive\r\n",
                        "User-Agent: Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.4 (KHTML, like Gecko) Chrome/5.0.375.55 Safari/533.4\r\n",
                        "Cache-Control: max-age=0\r\n",
                        "Accept: application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,image/png,*/*;q=0.5\r\n",
                        "Accept-Encoding: gzip,deflate,sdch\r\n",
                        "Accept-Language: en-US,en;q=0.8\r\n",
                        "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.3\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/", new Dictionary<string,string>()
                    {
                        {"Host", "localhost:8000"},
                        {"Connection", "keep-alive"},
                        {"User-Agent", "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US) AppleWebKit/533.4 (KHTML, like Gecko) Chrome/5.0.375.55 Safari/533.4"},
                        {"Cache-Control", "max-age=0"},
                        {"Accept", "application/xml,application/xhtml+xml,text/html;q=0.9,text/plain;q=0.8,image/png,*/*;q=0.5"},
                        {"Accept-Encoding", "gzip,deflate,sdch"},
                        {"Accept-Language", "en-US,en;q=0.8"},
                        {"Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.3"}
                    }, null), false)
                )
            },
            #endregion
            #region GET Dumbfool
            {
                new ParserTest(
                    "GET Dumbfool",
                    ASCII(
                        "GET /dumbfool HTTP/1.1\r\n",
                        "aaaaaaaaaaaaa:++++++++++\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/dumbfool", new Dictionary<string,string>()
                    {
                        {"aaaaaaaaaaaaa", "++++++++++"}
                    }, null), false)
                )
            },
            #endregion
            //FIXME - this test is going to change significantly when we add serious URL parsing
            #region GET Fragment in URL
            {
                new ParserTest(
                    "GET Fragment in URL",
                    ASCII(
                        "GET /forums/1/topics/2375?page=1#posts-17408 HTTP/1.1\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/forums/1/topics/2375?page=1#posts-17408", new Dictionary<string,string>()
                    {

                    }, null), false)
                )
            },
            #endregion
            #region GET No Headers No Body
            {
                new ParserTest(
                    "GET No Headers No Body",
                    ASCII(
                        "GET /get_no_headers_no_body/world HTTP/1.1\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/get_no_headers_no_body/world", new Dictionary<string,string>()
                    {

                    }, null), false)
                )
            },
            #endregion
            #region GET One Header No Body
            {
                new ParserTest(
                    "GET One Header No Body",
                    ASCII(
                        "GET /get_one_header_no_body HTTP/1.1\r\n",
                        "Accept: */*\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/get_one_header_no_body", new Dictionary<string,string>()
                    {
                        {"Accept", "*/*"}
                    }, null), false)
                )
            },
            #endregion
            #region GET Funky Content Length
            /*
            This is commented out because even though it's in ryah's test suite, it's not something
            that we can test nicely.  This is a bad request, and the parser's behavior here is 
            undefined at the moment.  If fed the whole request at once, Powernap will actually
            return a properly parsed request, and carry along.  If, however, some of the body is
            sent in a second AddBytes method, it will return a ParseError from that second call.
            {
                new ParserTest(
                    "GET Funky Content Length",
                    ASCII(
                        "GET /get_funky_content_length_body_hello HTTP/1.0\r\n",
                        "conTENT-Length: 5\r\n",
                        "\r\n",
                        "HELLO"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/get_funky_content_length_body_hello", new Dictionary<string,string>()
                    {

                    }, null), false)
                )
            }
            */
            #endregion
            #region POST Identity Body World
            {
                new ParserTest(
                    "POST Identity Body World",
                    ASCII(
                        "POST /post_identity_body_world?q=search#hey HTTP/1.1\r\n",
                        "Accept: */*\r\n",
                        "Transfer-Encoding: identity\r\n",
                        "Content-Length: 5\r\n",
                        "\r\n",
                        "World"
                    ),
                    new ParseResult(new Request(Methods.Post, Versions.HTTP1_1, "/post_identity_body_world?q=search#hey", new Dictionary<string,string>()
                    {
                        {"Accept", "*/*"},
                        {"Transfer-Encoding", "identity"},
                        {"Content-Length", "5"}
                    }, new MemoryStream(ASCII
                    (
                        "World"
                    ))), false)
                )
            },
            #endregion
            #region POST Large Entity (Regression)
            {
                new ParserTest(
                    "POST Large Entity (Regression)",
                    ASCII(
                        "POST / HTTP/1.1\r\n",
                        "Content-Length: " + (1024 * 1024 * 2).ToString() + "\r\n",
                        "\r\n",
                        CharRepeat('z', 1024 * 1024 * 2)
                    ),
                    new ParseResult(new Request(Methods.Post, Versions.HTTP1_1, "/", new Dictionary<string,string>()
                    {
                        {"Content-Length", (1024 * 1024 * 2).ToString()}
                    }, new MemoryStream(ASCII(
                        CharRepeat('z', 1024 * 1024 * 2)
                    ))), false)
                )
            },
            #endregion
            #region POST Chunked All Your Base
            {
                new ParserTest(
                    "POST Chunked All Your Base",
                    ASCII(
                        "POST /post_chunked_all_your_base HTTP/1.1\r\n",
                        "Transfer-Encoding: chunked\r\n",
                        "\r\n",
                        "1e\r\nall your base are belong to us\r\n",
                        "0\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Post, Versions.HTTP1_1, "/post_chunked_all_your_base", new Dictionary<string,string>()
                    {
                        {"Transfer-Encoding", "chunked"},
                    }, new MemoryStream(ASCII
                    (
                        "all your base are belong to us"
                    ))), false)
                )
            },
            #endregion
            #region POST Two Chunks Multiple Zero End
            {
                new ParserTest(
                    "POST Two Chunks Multiple Zero End",
                    ASCII(
                        "POST /two_chunks_mult_zero_end HTTP/1.1\r\n",
                        "Transfer-Encoding: chunked\r\n",
                        "\r\n",
                        "5\r\nhello\r\n",
                        "6\r\n world\r\n",
                        "000\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Post, Versions.HTTP1_1, "/two_chunks_mult_zero_end", new Dictionary<string,string>()
                    {
                        {"Transfer-Encoding", "chunked"},
                    }, new MemoryStream(ASCII
                    (
                        "hello world"
                    ))), false)
                )
            },
            #endregion
            #region POST Chunked with Trailing Headers
            {
                new ParserTest(
                    "POST Chunked with Trailing Headers",
                    ASCII
                    (
                        "POST /chunked_w_trailing_headers HTTP/1.1\r\n",
                        "Transfer-Encoding: chunked\r\n",
                        "\r\n",
                        "5\r\nhello\r\n",
                        "6\r\n world\r\n",
                        "0\r\n",
                        "Vary: *\r\n",
                        "Content-Type: text/plain\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Post, Versions.HTTP1_1, "/chunked_w_trailing_headers", new Dictionary<string,string>()
                    {
                        {"Transfer-Encoding", "chunked"},
                        {"Content-Type", "text/plain"},
                        {"Vary", "*"},
                    }, new MemoryStream(ASCII
                    (
                        "hello world"
                    ))), false)
                )
            },
            #endregion
            #region POST Chunked with Extensions
            {
                new ParserTest(
                    "POST Chunked with Extensions",
                    ASCII(
                        "POST /chunked_w_extensions HTTP/1.1\r\n",
                        "Transfer-Encoding: chunked\r\n",
                        "\r\n",
                        "5; ihatew3;what=aretheseparametersfor\r\nhello\r\n",
                        "6; blahblah; blah\r\n world\r\n",
                        "0\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Post, Versions.HTTP1_1, "/chunked_w_extensions", new Dictionary<string,string>()
                    {
                        {"Transfer-Encoding", "chunked"},
                    }, new MemoryStream(ASCII
                    (
                        "hello world"
                    ))), false)
                )
            },
            #endregion

            //Regression tests
            #region Content-Length: 0
            {
                new ParserTest(
                    "Content-Length: 0",
                    ASCII(
                        "GET / HTTP/1.1\r\n",
                        "Content-Length: 0\r\n",
                        "\r\n"
                    ),
                    new ParseResult(new Request(Methods.Get, Versions.HTTP1_1, "/", new Dictionary<string,string>()
                    {
                        {"Content-Length", "0"}
                    }, null), false)
                )
            },
            #endregion
        };
    }

    public class ParserTests
    {
        private static void CompareResults(ParseResult expected, ParseResult result)
        {
            //Disconnect handling
            Tests.Expect(expected.Close, result.Close, "Disconnect handling correct");

            //Response/Request testing
            if (result.Response == null && expected.Response != null)
            {
                Tests.Result(false, "Response Present");
            }
            else if (result.Request == null && expected.Request != null)
            {
                Tests.Result(false, "Request Present");
            }
            else
            {
                //Compare the response, if it's present
                if (result.Response != null)
                {
                    Tests.Expect(expected.Response.Name, result.Response.Name, "Response name correct");
                    Tests.Expect(expected.Response.Code, result.Response.Code, "Response HTTP code correct");
                }
                //Compare the request, if it's present
                else if (result.Request != null)
                {
                    //check request data
                    Tests.Expect(expected.Request.Version, result.Request.Version, "Request version correct");
                    Tests.Expect(expected.Request.Uri, result.Request.Uri, "Request URI correct");
                    Tests.Expect(expected.Request.Method, result.Request.Method, "Request method correct");

                    //check that headers in the response are correct, and should be present
                    foreach (KeyValuePair<string, string> pair in result.Request.Headers)
                        if (Tests.Result(expected.Request.Headers.ContainsKey(pair.Key), string.Format("Header {0} expected", pair.Key)))
                            Tests.Expect(expected.Request.Headers[pair.Key], pair.Value, string.Format("Header {0} correct", pair.Key));

                    //check that the response has all the headers it should
                    foreach (KeyValuePair<string, string> pair in expected.Request.Headers)
                        Tests.Result(result.Request.Headers.ContainsKey(pair.Key), string.Format("Header {0} present", pair.Key));

                    //check entities
                    if (result.Request.Entity != null && expected.Request.Entity == null)
                    {
                        Tests.Result(false, "Entity not present");
                    }
                    else if (result.Request.Entity == null && expected.Request.Entity != null)
                    {
                        Tests.Result(false, "Entity present");
                    }
                    else if (expected.Request.Entity != null)
                    {
                        if (Tests.Expect(expected.Request.Entity.Length, result.Request.Entity.Length, "Entity length correct"))
                        {
                            Tests.PushTest("Entities Equal");

                            //If we're reading from that static test list, we have to seek if we're not
                            //the first test to run, otherwise the stream will be at its end.
                            expected.Request.Entity.Seek(0, SeekOrigin.Begin);

                            int t, r;
                            for (int i = 0; i < expected.Request.Entity.Length; i++)
                            {
                                r = result.Request.Entity.ReadByte();
                                t = expected.Request.Entity.ReadByte();

                                if (r != t)
                                {
                                    Tests.Result(false, string.Format("Entities equal at byte {0}", i), string.Format("Expected {0}, got {1}", t, r));
                                    break;
                                }
                            }
                            Tests.PopTest();
                        }
                    }
                }
            }
        }

        public static void TestParser(IParser parser)
        {
            //Do tests, sending the entire body at once
            Tests.PushTest("Whole Request");
            foreach (ParserTest test in ParserTest.Tests)
            {
                Tests.PushTest(test.Name);

                ParseResult result = parser.AddBytes(new ArraySegment<byte>(test.Bytes));

                CompareResults(test.Result, result);

                //Prepare for the next parser test
                Tests.PopTest();
            }
            Tests.PopTest();

            //Do tests, sending one byte at a time
            Tests.PushTest("Byte-by-byte");
            foreach (ParserTest test in ParserTest.Tests)
            {
                Tests.PushTest(test.Name);

                ParseResult result = null;
                Tests.PushTest("Data input");
                for (int i = 0; i < test.Bytes.Length; i++)
                {
                    byte[] buffer = new byte[1];
                    buffer[0] = test.Bytes[i];
                    result = parser.AddBytes(new ArraySegment<byte>(buffer));
                    if (i != test.Bytes.Length - 1)
                        Tests.Expect(null, result, "Result is null");
                }
                Tests.PopTest();
                //the last parse result holds what we want
                CompareResults(test.Result, result);

                Tests.PopTest();
            }
        }
    }
}
