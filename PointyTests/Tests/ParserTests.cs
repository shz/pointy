using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Pointy;
using Pointy.HTTP;

//Tests to Add:
// - Safari
// - Chrome
// - IE
// - Tests that should fail

namespace PointyTests.Tests
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
        /// <summary>
        /// Holds our test cases.
        /// 
        /// These are adapted from ryah's HTTP parser tests (http://github.com/ry/http-parser/blob/master/test.c)
        /// </summary>
        public static readonly List<ParserTest> Tests = new List<ParserTest>()
        {
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
        };
    }

    public class ParserTests
    {
        public static void TestParser(IParser parser)
        {
            //Do tests, sending the entire body at once
            foreach (ParserTest test in ParserTest.Tests)
            {
                Console.WriteLine("\t" + test.Name);

                ParseResult result = parser.AddBytes(new ArraySegment<byte>(test.Bytes));

                if (!result.Close == test.Result.Close)
                    Console.WriteLine("\t\tFAIL: Improper disconnect handling");
                if (result.Response == null && test.Result.Response != null)
                {
                    Console.WriteLine("\t\tFAIL: Response was expected, but not returned");
                }
                else if (result.Request == null && test.Result.Request != null)
                {
                    Console.WriteLine("\t\tFAIL: Request was expected, but not returned");
                }
                else
                {
                    if (result.Response != null)
                    {
                        if (!result.Response.Name.Equals(test.Result.Response.Name))
                            Console.WriteLine("\t\tFAIL: Expected response name \"{0}\", got \"{1}\"", test.Result.Response.Name, result.Response.Name);
                        if (!result.Response.Name.Equals(test.Result.Response.Name))
                            Console.WriteLine("\t\tFAIL: Expected response number \"{0}\", got \"{1}\"", test.Result.Response.Number, result.Response.Number);
                    }
                    else if (result.Request != null)
                    {
                        //check request data
                        if (result.Request.Version != test.Result.Request.Version)
                            Console.WriteLine("\t\tFAIL: Expected response version \"{0}\", got \"{1}\"", test.Result.Request.Version, result.Request.Version);
                        if (!result.Request.Path.Equals(test.Result.Request.Path))
                            Console.WriteLine("\t\tFAIL: Expected response path {0}, got {1}", test.Result.Request.Path, result.Request.Path);
                        if (!result.Request.Method.Equals(test.Result.Request.Method))
                            Console.WriteLine("\t\tFAIL: Expected response method {0}, got {1}", test.Result.Request.Method, result.Request.Method);

                        //check headers
                        foreach (KeyValuePair<string, string> pair in result.Request.Headers)
                            if (!test.Result.Request.Headers.ContainsKey(pair.Key))
                                Console.WriteLine("\t\tFAIL: Should not have header \"{0}: {1}\"", pair.Key, pair.Value);
                            else if (!test.Result.Request.Headers[pair.Key].Equals(pair.Value))
                                Console.WriteLine("\t\tFAIL: Expected header \"{0}: {1}\", got \"{0}: {2}\"", pair.Key, test.Result.Request.Headers[pair.Key], pair.Value);
                        foreach (KeyValuePair<string, string> pair in test.Result.Request.Headers)
                            if (!result.Request.Headers.ContainsKey(pair.Key))
                                Console.WriteLine("\t\tFAIL: Expected header \"{0}: {1}\", but no such header was present", pair.Key, pair.Value);
                        
                        //check entities
                        if (result.Request.Entity != null && test.Result.Request.Entity == null)
                        {
                            Console.WriteLine("\t\tFAIL: Should not have request entity");
                        }
                        else if (result.Request.Entity == null && test.Result.Request.Entity != null)
                        {
                            Console.WriteLine("\t\tFAIL: Missing request entity");
                        }
                        else if (test.Result.Request.Entity != null)
                        {
                            if (test.Result.Request.Entity.Length != result.Request.Entity.Length)
                            {
                                Console.WriteLine("\t\tFAIL: Expected entity size {0}, got {1}", test.Result.Request.Entity.Length, result.Request.Entity.Length);
                            }
                            else
                            {
                                int t, r;
                                for (int i = 0; i < test.Result.Request.Entity.Length; i++ )
                                {
                                    r = result.Request.Entity.ReadByte();
                                    t = test.Result.Request.Entity.ReadByte();

                                    if (r != t)
                                    {
                                        Console.WriteLine("\t\tFAIL: Entities differ at byte {0}; expected {1}, got {2}", i, t, r);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
