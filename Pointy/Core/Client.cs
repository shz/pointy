using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pointy.Core
{
    /// <summary>
    /// Handles requests on a single socket
    /// </summary>
    class Client : IDisposable
    {
        const int BUFFER_SIZE = 1024 * 4; // 4KB
        readonly char[] QuerySep = new char[] { '?' };

        int Disposed = 0; // Bool doesn't work with CompareExchange
        Socket _UnderlyingSocket;
        Stream ClientStream;
        HTTP.Parser Parser;
        HTTP.Protocol Protocol;
        IRouter Router;
        Action<Action> Scheduler;

        // Parsing state.  Volatile, as while access to
        // them is serialized, they'll be accessed from
        // different threads and we want to ensure visibility.
        //
        // In reality, we don't need *all* of these to be
        // volatile.  I'll be cleaning them up as we go.
        // So...
        //
        // TODO - Re-check thread safety.  This baby's been
        //        refactored a whole bunch in the development
        //        process, enough that we should do another
        //        pass.  Many volatiles can probably be
        //        optimized away, if not all -- an explicit
        //        memory barrier ought to do the trick.
        volatile byte[] Buffer;
        volatile int Offset;
        volatile bool Autoread;
        volatile HTTP.Request Request;
        volatile HTTP.Response Response;
        volatile HTTP.Version Version;
        volatile String Method;
        volatile String Path;
        volatile Dictionary<string, string> Headers;
        volatile RequestDataAdapter DataAdapter;
        volatile bool Ready;

        public Stream Stream
        {
            get { return ClientStream; }
        }
        public Socket UnderlyingSocket
        {
            get { return _UnderlyingSocket; }
        }
        public bool IsDisposed
        {
            get { return Thread.VolatileRead(ref Disposed) == 1; }
        }

        public Client(Socket socket, Stream stream, HTTP.Protocol protocol, IRouter router, Action<Action> scheduler)
        {
            _UnderlyingSocket = socket;
            ClientStream = stream;
            Parser = new HTTP.Parser();
            Buffer = new byte[BUFFER_SIZE];
            Offset = 0;
            Protocol = protocol;
            Router = router;
            Scheduler = scheduler;

            Parser.OnRequestLine += RequestLine;
            Parser.OnHeader += Header;
            Parser.OnEndHeaders += EndHeaders;
            Parser.OnBody += Body;
            Parser.OnEnd += EndRequest;
            Parser.OnParseError += ParseError;
        }
        ~Client()
        {
            Dispose();
        }
        public void Dispose()
        {
            // Only dispose if it hasn't already happened
            if (0 == Interlocked.CompareExchange(ref Disposed, 1, 0))
            {
                ClientStream.Dispose();
                GC.SuppressFinalize(this);
            }
            
        }

        #region Reading Code

        async Task ReadAsync()
        {
            try
            {
                var bytes = await ClientStream.ReadAsync(Buffer, Offset, BUFFER_SIZE - Offset);
                if (bytes == 0)
                {
                    Kill();
                }
                else
                {
                    var origOffset = Offset;
                    var origBuffer = Buffer;
                    if (BUFFER_SIZE - Offset > bytes)
                    {
                        Offset += bytes;
                    }
                    else
                    {
                        Buffer = new byte[BUFFER_SIZE];
                        Offset = 0;
                    }
                    Parser.AddBytes(new ArraySegment<byte>(origBuffer, origOffset, bytes));
                }
            }
            catch (IOException)
            {
                Kill();
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }

        void Read()
        {
            try
            {
                ClientStream.BeginRead(Buffer, Offset, BUFFER_SIZE - Offset, EndRead, null);
            }
            catch (IOException)
            {
                Dispose();
            }
        }
        void EndRead(IAsyncResult result)
        {
            int bytes = 0;
            try
            {
                bytes = ClientStream.EndRead(result);
            }
            catch (IOException err)
            {
                Kill();
                return;
            }
            // If we were disposed while waiting for a read to go through, then something else
            // killed the request/response and we don't have to do anything.
            catch (ObjectDisposedException)
            {
                return;
            }

            // If we read no bytes, then the client disconnected and we should cancel
            // the request and response.
            if (bytes == 0)
            {
                Kill();
            }
            // Otherwise, parse the data out
            else
            {
                // Continue to fill up the old buffer if we haven't used the whole of it.  Do
                // this BEFORE shooting the bytes through the parser to ensure that if another
                // Read is queued, it will have the correct offset.
                var origOffset = Offset;
                var origBuffer = Buffer;
                if (BUFFER_SIZE - Offset > bytes)
                {
                    Offset += bytes;
                }
                else
                {
                    Buffer = new byte[BUFFER_SIZE];
                    Offset = 0;
                }
                Parser.AddBytes(new ArraySegment<byte>(origBuffer, origOffset, bytes));

                // If, once we're done parsing, we have a response ready to fire off, do it
                if (Request != null)
                {
                    // Hold on to these, because they'll get cleared
                    var req = Request;
                    var res = Response;

                    // Clear out request to prevent it from being queued again.
                    Request = null;

                    // Execute the response
                    Scheduler(async delegate
                    {
                        // Need to wrap this initial call in a try so that
                        // we can handle errors by killing the connection.
                        try
                        {
                            var action = Router.Resolve(req);
                            if (action != null)
                            {
                                await action(req, res);
                            }
                            else
                            {
                                await WriteError(404, "Page Not Found").ContinueWith(delegate(Task t)
                                {
                                    if (t.IsCanceled || t.IsFaulted)
                                    {
                                        Kill();
                                    }
                                    else
                                    {
                                        Start();
                                    }
                                });
                            }
                        }
                        catch (Exception)
                        {
                            Kill();
                            throw;
                        }
                    });
                }

                // If we still need to keep reading automatically, do that
                if (Autoread)
                    Read();
            }
        }

        #endregion

        #region Low-Level Error Response

        async Task WriteError(int code, string reason)
        {
            using (StreamWriter writer = new StreamWriter(ClientStream, Encoding.ASCII, 1024, true))
            {
                var content = Encoding.ASCII.GetBytes(String.Format("{0} {1}", code, reason));

                await writer.WriteAsync("HTTP/" + (Version == HTTP.Version.HTTP1_0 ? "1.0" : "1.1") + " " + code.ToString() + " " + reason + "\r\n");
                await writer.WriteAsync("Server: Pointy/3.1415\r\n");
                await writer.WriteAsync("Content-Length: 0\r\n");
                await writer.WriteAsync(String.Format("Date: {0}\r\n", DateTime.UtcNow.ToString("R")));
                await writer.WriteAsync("\r\n");
            }
        }

        #endregion

        // The client state machine

        void Kill()
        {
            // Stop the response in its tracks.  This will trigger the done
            // callback, which will similarly kill any reading on the request.
            // Note that this will actually flow back and cause Kill() to be
            // executed a second time; this is fine, because both the response
            // and our Dispose method gracefully handle meing called multiple
            // times.
            if (Response != null)
            {
                var temp = Response;
                Response = null;
                temp.Abort();
            }
            // This will wipe out the socket, stopping any current reading
            // in its tracks.
            Dispose();
        }
        public void Start()
        {
            // Set up the state
            Ready = true;
            Autoread = true;
            Request = null;
            Response = null;
            Version = HTTP.Version.HTTP1_0;
            Method = null;
            Path = null;
            Headers = null;
            DataAdapter = null;

            // If there's already a reader chugging away, simply setting
            // autoread will take care of it.  Otherwise, we need to restart
            // the reading.
            Read();
        }

        #region Parser Event Handlers

        public async void ParseError(HTTP.Parser.Error error, string extra)
        {
            // We don't survive HTTP errors on the socket, so we can effectively
            // stop listening.  The data coming in is just sent to a black hole,
            // but at least we can save some processing this way.
            Autoread = false;

            // If we have a request we're working with, it means parse errors are going
            // to come from the body.  This is theoretically possible to handle cleanly
            // if the response hasn't started yet, but to make our lives easier we're
            // just going to kill it uncleanly.
            if (Request != null)
            {
                Kill();
                return;
            }

            // Otherwise, we can safely create a response for this socket without
            // ruining everything.  We can even use async; we'll be the only one
            // using this socket until we kill it.  So even though we don't have
            // a SynchronizationContext set in the ThreadPool (or do we?), we
            // don't care because the async functionality will effectively serialize
            // the actions for us as we write out the response.
            int code = 0;
            string reason = null;
            switch (error)
            {
                case HTTP.Parser.Error.HeaderFieldsTooLarge:
                    code = 431;
                    reason = "Request Header Fields Too Large";
                    break;
                case HTTP.Parser.Error.UriTooLong:
                    code = 414;
                    reason = "Request-URI Too Long";
                    break;
                case HTTP.Parser.Error.BadRequest:
                default:
                    code = 400;
                    reason = "Bad Request";
                    break;
            }
            await WriteError(code, reason);

            // And when we end the response, it's time to die
            Kill();
        }
        public void RequestLine(string method, string path, HTTP.Version version)
        {
            // If another request starts before we're ready for a new one, then
            // the client is attempting to pipeline.  This is a bad idea, so we
            // punish them by blowing up.
            if (!Ready)
            {
                Kill();
                return;
            }
            Ready = false;

            Method = method;
            Path = path;
            Version = version;
            Headers = new Dictionary<string, string>();
        }
        public void Header(string name, string value)
        {
            // If the request is not null, then this is a trailer.  We
            // don't really have a good way to handle it, so for now
            // it'll just be swallowed.
            if (Request != null)
                return;

            name = name.ToLower();
            Headers[name] = value;
        }
        public void EndHeaders()
        {
            // Now that headers are over, all reading should be manual
            Autoread = false;

            // Prep the request
            DataAdapter = new Core.RequestDataAdapter(async delegate
            {
                await ReadAsync();
            });
            Request = new HTTP.Request(DataAdapter);
            Request.Headers = Headers;
            Request.Method = Method;
            Request.Protocol = Protocol;

            // Parse out the URI, which ends up being more annoying than it
            // should be.  Stay tuned...
            var uri = new Uri(Path, UriKind.RelativeOrAbsolute);

            // Set the path
            if (uri.IsAbsoluteUri)
                Request.Path = uri.AbsolutePath;
            else
                Request.Path = Path.Split('?')[0];

            // Set the host
            string temp;
            if (Headers.TryGetValue("host", out temp))
                Request.Host = temp;
            else if (uri.IsAbsoluteUri)
                Request.Host = uri.Host;
            else
                Request.Host = null;

            // Set the query
            if (uri.IsAbsoluteUri)
            {
                Request.Query = Util.FormUrlEncoding.Parse(uri.Query.Length > 0 ? uri.Query.Substring(1) : "");
            }
            else
            {
                var split = Path.Split(QuerySep, 2);
                Request.Query = Util.FormUrlEncoding.Parse(split.Length > 1 ? split[1] : "");
            }

            // Build the response
            bool keepAlive = Version == HTTP.Version.HTTP1_1;
            if (Headers.TryGetValue("connection", out temp))
            {
                temp = temp.ToLower();
                if (temp == "close")
                    keepAlive = false;
                else if (temp == "keep-alive")
                    keepAlive = true;
            }
            Response = new HTTP.Response(Version, this, keepAlive, delegate(bool forceDisconnect)
            {
                // Now that we're done, we should kill the client if
                // either the request expects it, or the response
                // requires it.
                if (forceDisconnect || !keepAlive)
                    Kill();
                else
                    Start();
            });

            // Since we've set up the request, it'll get scheduled at the END of the current
            // read call.  This prevents issues with overlapping reads, because reads are
            // guaranteed to have finished before we run any response logic.
        }
        public void Body(ArraySegment<byte> data)
        {
            if (DataAdapter != null)
                DataAdapter.AddData(data);
        }
        public void EndRequest()
        {
            if (DataAdapter != null)
                DataAdapter.AddData(new ArraySegment<byte>(Buffer, 0, 0));
        }

        #endregion
    }
}
