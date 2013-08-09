# Pointy Release Notes

Listed in reverse chronological order.  Pointy versioning follows
[semver](http://semver.org/).

## TODO (i.e. future work)

In no specific order, here are things that need to be done:

 * Streaming multipart parser
 * Low-fruit profiling pass
 * In-depth profiling of the HTTP parser
 * Full XML docs
 * Full documentation
 * Fold advanced routing discovery into static route approach

## Version 1.0 (Future)

Fixes a few important bugs:

 * Dead client reaping
 * Volatile mess in Client implementation
 * Mono kinks worked out
 * Response prevents accidental bad usage

Adds a few minor features:

 * More default MIME types

Adds a bit of testing help:

 * Testing regimen documentation and supporting shell scripts
 * Unit tests
   * Parser
	 * Utility classes
	 * Routers

And builds out some documentation:

 * Full XML doc coverage for public API
 * "Enough" user documentation

## Version 0.9

A complete ground-up rewrite of the entire server architecture and
user API.  Now built around C#5 `async`, implemented with an internal
dedicated thread pool and an intelligent queueing approach.

Simple, yet very powerful and flexible routing system.

Refactor of the HTTP parser, losing some features that may need to
be added back in (100-Continue support and trailers).

A couple bug fixes here and there, and more misc changes to support
all the above.  Basically, this thing's a whole new library.