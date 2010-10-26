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

namespace PointyTests
{
    class PointyUriTests
    {
        public static void Run()
        {
            Pointy.Util.PointyUri uri;

            Tests.PushTest("Google URL");
                uri = new Pointy.Util.PointyUri("http://www.google.com/");
                Tests.Expect(null, uri.Fragment, "Fragment is null");
                Tests.Expect("www.google.com", uri.Host, "Host is correct");
                Tests.Expect("/", uri.Path, "Path is correct");
                Tests.Expect(80, uri.Port, "Port is 80");
            Tests.PopTest();

            Tests.PushTest("Many Segments");
                uri = new Pointy.Util.PointyUri("http://example.com/foo/bar/baz/bam/whats.it");
                Tests.Expect(null, uri.Fragment, "Fragment is null");
                Tests.Expect("example.com", uri.Host, "Host is correct");
                Tests.Expect("/foo/bar/baz/bam/whats.it", uri.Path, "Path is correct");
                Tests.Expect(80, uri.Port, "Port is 80");
            Tests.PopTest();

            Tests.PushTest("Fragment");
                uri = new Pointy.Util.PointyUri("http://foobar.com/pet/wolverine/#wat");
                Tests.Expect("wat", uri.Fragment, "Fragment is correct");
                Tests.Expect("foobar.com", uri.Host, "Host is correct");
                Tests.Expect("/pet/wolverine/", uri.Path, "Path is correct");
                Tests.Expect(80, uri.Port, "Port is 80");
            Tests.PopTest();

            Tests.PushTest("Query");
                uri = new Pointy.Util.PointyUri("http://example.com/?foo=bar&baz");
                //todo
            Tests.PopTest();

            Tests.PushTest("Everything");
                uri = new Pointy.Util.PointyUri("https://abc.123.web.it/hang/on/to/?your=hats#folks");
                //heh, we'll just assume that no exception = proper parsing for now...
            Tests.PopTest();
            
        }
    }
}
