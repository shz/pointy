=  Pointy 1.0 =
A fast, lightweight HTTP frontend for .NET.

== About ==
Pointy is still under development.  The API will be fluctuating until version 1.0, at which point
it will remain stable and backwards-compatible for the entirety of the 1.x series (which may well
be the only release series to exist, but hey).

Pointy is intended to be an HTTP interface for web applications.  It's not a framework, though it
can still be used to create simple web applications without much boilerplate code.

Pointy is coded entirely in C# 2.0, and runs without a hitch on Mono.

=== Features ===
Aside from sporting a simple and clean API, Pointy has the ability to do "streaming" HTTP
requests.  This is accomplished via chunked encoding for HTTP/1.1, or by proper manipulation
of HTTP/1.0 connections.  This streaming functionality allows Pointy to send out data as
it receives it, without buffering.  This is rather `node.js`-like, at least on the response end
of things.  Requests must still be fully received before Pointy passes them along to the
application.

Pointy's networking design is also "modern;" the asynchronous socket API is used, allowing
easily scalable concurrency.  This sort of thing is actually pretty easy to do in .NET,
but it's worth mentioning.

=== Performance ===
Speed is a major priority for Pointy.  Here's a benchmark of Pointy 8.1 (lacking some
significant optimizations introduced in 0.9) against Tornado 0.2 (at the time of
writing, one of the fastest Python web servers available) and node.js 0.1.27 (generally
one of the fastest HTTP servers period):

    ab -c 30 -n 10000
    node.js: ~2550 requests/second
    Tornado: ~1600 requests/second
    Pointy:  ~1800 requests/second
	
This test simply involved serving "Hello World" to any response; it's more a test of
the inner HTTP plumbing of these tools than anything else.

Note that Pointy has received some significant optimizations since this test was run, so
its performance should be even closer to that of node.js.

See `notes.txt` for release notes.

== Documentation ==
Documentation is a bit of a WIP right now.  There *will* be comprehensive docs at some point
in the not-so-distance future.  In the meantime, use the XML docs and the example projects
(located, shockingly, in the `Examples` folder).

== License ==
Pointy and its test code are released under the MIT X11 license.
*An exception is Pointy's Ragel-based URI parser, which is released into the public domain.*

Pointy's examples are released into the public domain.

See license.txt and individual source files for details.

