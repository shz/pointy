using System;
using System.Collections.Generic;
using System.Text;

//TODO - these obviously need some real tests :)

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
