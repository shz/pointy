#  Pointy 0.9

A fast, lightweight HTTP frontend for .NET, inspired by `node.js`.

# Example

```C#
using System;

using Pointy;
using Pointy.Routers;

namespace Awesome
{
    class Program
    {
				static volatile bool Running = true;

        static void Main(string[] args)
        {
            var router = new CatchAllRouter(async (req, res) =>
            {
                await res.Start(200);
                await res.Write(data);
                await res.Finish();
            });

            using (var server = new Server(router))
            {
                server.Run(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888));
                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    Running = false;                
								};
                while (Running) System.Threading.Thread.Sleep(100);
            }
        }
    }
}
```

# About

Everyone loves lists, so I'm going to shoot some bullet points your way.  Pointy's
built with the following primary goals:

 * Blazing fast speed
 * High concurrency
 * Low-level HTTP access
 * Simple, delightful API
 * Request/response streaming (ala `node.js`)
 * Simple, flexible request routing

That's Pointy proper.  On top of that, I've written (or I'm in the process of writing) a set of
utilities that meets all the above goals and also provides:

 * Multipart parsing
 * MimeType tools
 * x-www-form-urlencoded encoding/decoding
 * High-level request/response handling
 * Built-in file serving
 * OWIN support

Or basically, everything you need to start writing real webapps.  Use the high-level API for most
parts, and then hop down into the low-level interface for the fancy bits.  Parse forms with the
built in utilities.  Use existing OWIN-based software with Pointy's fast serving.

Pointy's written in C# for the .NET framework 4.5, and compiles on Mono without a hitch.  There
are currently a couple Mono-specific run time showstoppers that I'll be ironing out shortly.

# Documentation

That's always the kicker, eh?  Documentation is a WIP right now.  There *will* be comprehensive
HTML docs at some point, once the features are closer to done and stable.  In the meantime, take
a gander at the XML docs and check out the example projects (located, shockingly, in the `Examples`
folder).

# Testing

See `testing.md` for details.  The short version: NUnit excercises the utility code, the
parser, the routing, and some of the request/response logic.  The network code, threading,
the request of the request/response logic, etc. is best tested using HTTP benchmarking
utilities.

# License

Pointy, its test code, and documentation are all released into the public domain.
See license.txt for details.

Go nuts!
