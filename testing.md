# Testing

Things that can be unit tested well are handled via the PointyTests project, which itself
uses NUnit.  Here's an exhaustive list of what that covers

 * Nothing right now :(

# Everything else

This still leaves us with the server networking logic, and some of the more
nuanced/network-related request/response code, as well as the whole issue of
thread safety.

The best way to excercise these bits is to just throw high amounts of
concurrent load at a server.  Fire up one or two of the examples and then
hit it with `ab` or `siege` or `httperf` or whatever, and check to make
sure it all works.  The key part is to load the server for a few seconds
at high concurrency to try to flush out any threading or disposal issues.

Some useful examples:

```bash
ab -k -n 3000 -c 100 http://localhost:8888/
```

```bash
wrk -t40 -c1000 -d20s http://localhost:8888/
```
