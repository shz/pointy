using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;

namespace Pointy.HTTP
{
    /// <summary>
    /// Attempted to write data to the response in a manner that doesn't conform to the HTTP standard.
    /// 
    /// This is likely to result from incorrectly ordering Response method calls.
    /// </summary>
    [Serializable]
    public class HttpViolationException : Exception
    {
        public HttpViolationException(string message) : base(message)
        {

        }
    }

    public class Response
    {
        /// <summary>
        /// HTTP-style header dictionary.  Uses a list of Tuples internally, allowing
        /// for duplicates.  Reading from it is relatively slow, writing to it is
        /// relatively fast.
        /// </summary>
        class ResponseHeaders : IDictionary<string, string>
        {
            Response Res;
            List<Tuple<string, string>> Headers = new List<Tuple<string, string>>();

            public ResponseHeaders(Response res)
            {
                Res = res;
            }

            /// <summary>
            /// Writes all headers to a stream asynchronously.  Does not
            /// write final trailing CRLF.
            /// </summary>
            /// <param name="stream">Stream to write to</param>
            /// <returns></returns>
            public async Task Write(Stream stream)
            {
                foreach (var tuple in Headers)
                {
                    var bytes = Encoding.ASCII.GetBytes(String.Join("", tuple.Item1, ": ", tuple.Item2, "\r\n"));
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }

            #region IDictionary

            public void Add(string key, string value)
            {
                Headers.Add(new Tuple<string, string>(key, value));
            }
            public void Add(KeyValuePair<string, string> item)
            {
                Add(item.Key, item.Value);
            }
            public bool Remove(KeyValuePair<string, string> item)
            {
                return Headers.Remove(new Tuple<string, string>(item.Key, item.Value));
            }
            public bool Remove(string key)
            {
                return Headers.RemoveAll((tuple) => tuple.Item1 == key) > 0;
            }
            public bool ContainsKey(string key)
            {
                foreach (var tuple in Headers)
                    if (tuple.Item1 == key)
                        return true;
                return false;
            }
            public bool TryGetValue(string key, out string value)
            {
                value = null;
                foreach (var tuple in Headers)
                {
                    if (tuple.Item1 == key)
                    {
                        if (value != null) value += "\n" + tuple.Item2;
                        else value = tuple.Item2;
                    }
                }
                return value != null;
            }
            public bool Contains(KeyValuePair<string, string> item)
            {
                return Headers.Contains(new Tuple<string, string>(item.Key, item.Value));
            }
            public void Clear()
            {
                Headers.Clear();
            }
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                foreach (var tuple in Headers)
                    yield return new KeyValuePair<string, string>(tuple.Item1, tuple.Item2);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public ICollection<string> Keys
            {
                get
                {
                    SortedSet<string> keys = new SortedSet<string>();
                    foreach (var tuple in Headers)
                        keys.Add(tuple.Item1);

                    return keys;
                }
            }
            public ICollection<string> Values
            {
                get { throw new NotImplementedException(); }
            }
            public int Count
            {
                get { return Values.Count; }
            }
            public bool IsReadOnly
            {
                get { return false; }
            }
            public string this[string key]
            {
                get
                {
                    string val;
                    if (!TryGetValue(key, out val))
                        throw new KeyNotFoundException();
                    return val;
                }
                set
                {
                    Add(key, value);
                }
            }

            #endregion
        }

        /// <summary>
        /// Current state of the response
        /// </summary>
        enum ResponseState
        {
            ResponseLine,
            Headers,
            Trailers,
            Body,
            Done,
            Error,
            Cancelled
        }

        /// <summary>
        /// CRLF in binary (ASCII) form
        /// </summary>
        static readonly byte[] CRLF = new byte[2]
        {
            0xD, 0xA
        };

        ResponseState State = ResponseState.ResponseLine;
        Core.Client Client;
        Action<bool> DoneCallback;
        HTTP.Version Version;
        int ContentLength = -1;
        bool ForceDisconnect = false;
        ResponseHeaders _Headers;

        internal Response(HTTP.Version version, Core.Client client, bool keepAlive, Action<bool> doneCallback)
        {
            Client = client;
            DoneCallback = doneCallback;
            Version = version;
            _Headers = new ResponseHeaders(this);
            if (keepAlive)
                _Headers["Connection"] = "keep-alive";
        }

        /// <summary>
        /// DRY; Wraps common Socket.Send functionality
        /// </summary>
        /// <returns></returns>
        Task Send(string data)
        {
            // The encoder call may or may not throw an EncoderFallbackException.
            // I don't really care. If someone's giving us non-ASCII data, they deserve it.
            return Send(Encoding.ASCII.GetBytes(data));
        }
        /// <summary>
        /// DRY; Wraps common Socket.Send functionality
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task Send(byte[] data)
        {
            return Send(new ArraySegment<byte>(data));
        }
        /// <summary>
        /// DRY; Wraps common Socket.Send functionality
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task Send(ArraySegment<byte> data)
        {
            if (State == ResponseState.Error)
                throw new InvalidOperationException("Response encountered an error and cannot continue");
            else if (State == ResponseState.Cancelled)
                throw new InvalidOperationException("Client disconnected");

            // If the socket dies due to a client disconnect mid-response, this
            // will raise a nasty exception.  We don't want to handle this; it's
            // up to the user.
            return Client.Stream.WriteAsync(data.Array, data.Offset, data.Count).ContinueWith(delegate(Task t)
            {
                // If something went wrong, bail out
                if (t.IsFaulted || t.IsCanceled)
                    Abort();
            });
        }

        /// <summary>
        /// Begins sending an HTTP 200 OK response to the client.
        /// </summary>
        public Task Start()
        {
            return Start(200);
        }
        /// <summary>
        /// Begins sending an HTTP response to the client with the specified status code and the default
        /// reason phrase for that code.
        /// </summary>
        /// <param name="code"></param>
        public Task Start(int code)
        {
            // This here switch statement calls StartResponse(int, string)
            // with the code and the default reason phrase.  All codes from
            // RFC2616 are implemented.
            //
            // These phrases are matched to their codes directly in the
            // switch.  Refactor the matching into its own method if this
            // list is needed elsewhere.
            switch (code)
            {
                #region 100s

                case 100:
                    return Start(code, "Continue");
                case 101:
                    return Start(code, "Switching Protocols");

                #endregion
                #region 200s

                case 200:
                    return Start(code, "OK");
                case 201:
                    return Start(code, "Created");
                case 202:
                    return Start(code, "Accepted");
                case 203:
                    return Start(code, "Non-Authoritative Information");
                case 204:
                    return Start(code, "No Content");
                case 205:
                    return Start(code, "Reset Content");
                case 206:
                    return Start(code, "Partial Content");

                #endregion
                #region 300s

                case 300:
                    return Start(code, "Multiple Choices");
                case 301:
                    return Start(code, "Moved Permanently");
                case 302:
                    return Start(code, "Found");
                case 303:
                    return Start(code, "See Other");
                case 304:
                    return Start(code, "Not Modified");
                case 305:
                    return Start(code, "Use Proxy");
                case 307:
                    return Start(code, "Temporarly Redirect");

                #endregion
                #region 400s

                case 400:
                    return Start(code, "Bad Request");
                case 401:
                    return Start(code, "Unauthorized");
                case 402:
                    return Start(code, "Payment Required");
                case 403:
                    return Start(code, "Forbidden");
                case 404:
                    return Start(code, "Not Found");
                case 405:
                    return Start(code, "Method Not Allowed");
                case 406:
                    return Start(code, "Not Acceptable");
                case 407:
                    return Start(code, "Proxy Authentication Required");
                case 408:
                    return Start(code, "Request Time-out");
                case 409:
                    return Start(code, "Conflict");
                case 410:
                    return Start(code, "Gone");
                case 411:
                    return Start(code, "Length Required");
                case 412:
                    return Start(code, "Precondition Failed");
                case 413:
                    return Start(code, "Request Entity Too Large");
                case 414:
                    return Start(code, "Request-URI Too Large");
                case 415:
                    return Start(code, "Unsupported Media Type");
                case 416:
                    return Start(code, "Requested range not satisfiable");
                case 417:
                    return Start(code, "Expectation Failed");

                #endregion
                #region 500s

                case 500:
                    return Start(code, "Internal Server Error");
                case 501:
                    return Start(code, "Not Implemented");
                case 502:
                    return Start(code, "Bad Gateway");
                case 503:
                    return Start(code, "Service Unavailable");
                case 504:
                    return Start(code, "Gateway Time-out");
                case 505:
                    return Start(code, "HTTP Version not supported");

                #endregion

                default:
                    return Start(code, "Hi Mom"); // If someone's using a crazy code without
                                                  // supplying a reason phrase, they deserve it.
            }
        }
        /// <summary>
        /// Begins sending an HTTP response to the client with the specified status code and reason phrase.
        /// </summary>
        /// <param name="code">HTTP status code (See RFC2616 6.1.1)</param>
        /// <param name="reasonPhrase">Reason phrase</param>
        public Task Start(int code, string reasonPhrase)
        {
            // Do some sanity checking on state
            if (State != ResponseState.ResponseLine)
            {
                if (State == ResponseState.Done)
                    throw new HttpViolationException("Response has already been sent to the client");
                else
                    throw new HttpViolationException("Response has already been started");
            }

            // We're writing headers next
            State = ResponseState.Headers;

            // Send the Status-Line (RFC2616 6.1)
            return Send(string.Concat("HTTP/", Version == HTTP.Version.HTTP1_0 ? "1.0 " : "1.1 ", code, " ", reasonPhrase, "\r\n"));
        }

        /// <summary>
        /// Sets and gets response headers
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get { return _Headers; }
        }
        /// <summary>
        /// Writes the header dictionary to the response
        /// </summary>
        /// <returns></returns>
        private async Task WriteHeaders()
        {
            // Add a server header if none was specified
            string temp;
            if (!Headers.TryGetValue("Server", out temp))
                Headers["Server"] = "Pointy/3.1415";
            if (!Headers.TryGetValue("Date", out temp))
                Headers["Date"] = DateTime.UtcNow.ToString("R");

            // Send the headers
            try
            {
                await _Headers.Write(Client.Stream);
            }
            catch (Exception err)
            {
                Abort();
                return;
            }

            // Signal end of headers with CRLF
            await Send(CRLF);
        }

        /// <summary>
        /// Handles Content-Length, writing headers, etc
        /// </summary>
        /// <returns></returns>
        private async Task StartBody()
        {
            // This gets used in a couple places for reading headers
            string temp;

            // Parse out content length
            if (Headers.TryGetValue("Content-Length", out temp))
            {
                if (!Int32.TryParse(temp, out ContentLength))
                {
                    throw new HttpViolationException("Invalid Content-Length header");
                }
            }
            else
            {
                ContentLength = -1;
            }


            // If there's no content length then we should use chunked mode.  We
            // have some bookkeeping to do.
            if (ContentLength < 0)
            {
                // For HTTP/1.1, we need to ensure Transfer-Encoding is
                // set to chunked.  If the user hasn't already set that
                // header, do it for them.  A nice bonus is that by RFC2616,
                // if Transfer-Encoding is sent it must include chunked, and
                // chunked must be the last encoding applied.  This means
                // that unless the user is breaking the spec, our chunk
                // handling WILL work.
                // FIXME - Not entirely true, as we can use transfer-encoding
                //         without chunks by killing the connection.  We
                //         should support this in the future.
                if (Version == HTTP.Version.HTTP1_1)
                {
                    if (!Headers.TryGetValue("Transfer-Encoding", out temp))
                        Headers["Transfer-Encoding"] = "chunked";

                    // TODO - Verify the user-defined Transfer-Encoding
                }
                // Unfortunately, HTTP/1.0 doesn't support transfer encoding.
                // We can work around this by ensuring that we close the
                // connection when we're done sending the body, in which case
                // the Content-Length header isn't required and we can send
                // whatever we want to.
                else
                {
                    ForceDisconnect = true;
                    // Must remove the header to ensure we don't mess with clients
                    _Headers.Remove("Connection");
                }
            }

            // Write the headers
            await WriteHeaders();

            // Update the state
            State = ResponseState.Body;
        }
        /// <summary>
        /// Sends a string to the client, using UTF-8 encoding
        /// </summary>
        /// <param name="data">String to send</param>
        /// <returns></returns>
        public Task Write(string data)
        {
            return Write(data, Encoding.UTF8);
        }
        /// <summary>
        /// Sends a string to the client, using the specified encoding
        /// </summary>
        /// <param name="data">String to send</param>
        /// <param name="encoding">Encoding to use</param>
        /// <returns></returns>
        public Task Write(string data, Encoding encoding)
        {
            return Write(encoding.GetBytes(data));
        }
        /// <summary>
        /// Sends raw data to the client.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public Task Write(byte[] data)
        {
            return Write(new ArraySegment<byte>(data));
        }
        /// <summary>
        /// Sends raw data to the client.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Write(ArraySegment<byte> data)
        {
            // Make sure we're in the right response state
            if (State == ResponseState.Done)
                throw new HttpViolationException("Response has already been sent to the client");
            else if (State == ResponseState.ResponseLine)
                throw new HttpViolationException("Response has not been started yet");
            else if (State == ResponseState.Trailers)
                throw new HttpViolationException("Can't send more body data after trailers");

            // If we're in the header state, transition to the body phase
            if (State == ResponseState.Headers)
                await StartBody();

            // If we're in chunked mode, send the chunk length
            if (ContentLength < 0 && Version == HTTP.Version.HTTP1_1)
                await Send(string.Format("{0:x}\r\n", data.Count));

            // Write ze data
            await Send(data);

            // If we're in chunked mode, send another CRLF to polish off the chunk
            if (ContentLength < 0 && Version == HTTP.Version.HTTP1_1)
                await Send(CRLF);
        }
        /// <summary>
        /// Writes a file to the response.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task WriteFile(FileInfo file)
        {
            // Make sure we're in the right response state
            if (State == ResponseState.Done)
                throw new HttpViolationException("Response has already been sent to the client");
            else if (State == ResponseState.ResponseLine)
                throw new HttpViolationException("Response has not been started yet");
            else if (State == ResponseState.Trailers)
                throw new HttpViolationException("Can't send more body data after trailers");

            // If we're in the header state, transition to the body phase
            if (State == ResponseState.Headers)
                await StartBody();

            // If we're in chunked mode, send the chunk length
            if (ContentLength < 0 && Version == HTTP.Version.HTTP1_1)
                await Send(string.Format("{0:x}\r\n", file.Length));

            // And now the logic gets funky.  If the underlying stream is an SSL
            // stream then we have to do the file reading manually.  However, if
            // it's a plain-jane Socket, we can use the uber-fast WriteFile method
            // to do our work for us.
            if (Client.UnderlyingSocket != null)
            {
                await Task.Factory.FromAsync(Client.UnderlyingSocket.BeginSendFile,
                    Client.UnderlyingSocket.EndSendFile,
                    file.FullName,
                    null);
            }
            else
            {
                throw new NotImplementedException("Soon.");
            }

            // If we're in chunked mode, send another CRLF to polish off the chunk
            if (ContentLength < 0 && Version == HTTP.Version.HTTP1_1)
                await Send(CRLF);
        }
        
        /// <summary>
        /// Finishes sending the response to the client.
        /// </summary>
        /// <returns></returns>
        public async Task Finish()
        {
            // Make sure we're in the right response state
            if (State == ResponseState.ResponseLine)
                throw new Exception("Response has not been started yet");
            else if (State == ResponseState.Done)
                throw new Exception("Response has already been sent to the client");

            // If we're still in headers, it means no body was sent.  We
            // need to send the headers before we wrap the request up. We
            // also need to force Content-Length to 0.
            if (State == ResponseState.Headers)
            {
                Headers["Content-Length"] = "0";
                await WriteHeaders();
            }
            // Otherwise, we need to wrap up sending the body
            else if (State == ResponseState.Body)
            {
                // If we're using chunked encoding and haven't sent any
                // trailers, we need to send that final chunk before
                // signalling the end of the request.
                if (ContentLength < 0 && Version == HTTP.Version.HTTP1_1)
                    await Send("0\r\n");

                // All the cases converge to requiring one more CRLF
                await Send(CRLF);
            }

            // Update the state to prevent any more writing on this Response
            State = ResponseState.Done;

            // If the Connection header was specified with a value other than
            // Keep-Alive, we should close the connection.
            string s;
            if (_Headers.TryGetValue("Connection", out s) && s.ToLower() != "keep-alive")
                ForceDisconnect = true;

            // Trigger the completion callback
            DoneCallback(ForceDisconnect);
        }

        /// <summary>
        /// Immediately stops writing the response to the client, in an ungraceful manner.
        /// 
        /// This method should only be used when the user code has encountered an
        /// error from which it cannot recover, while already writing a response.
        /// Whenever possible, user code should attempt to make proper use of HTTP
        /// status codes (likely 500-505) to gracefully handle errors.
        /// </summary>
        public void Abort()
        {
            // If we're either cancelled or errored, there's no need to do anything
            if (State == ResponseState.Error || State == ResponseState.Cancelled)
                return;

            // Kill the connection and wrap up here
            State = ResponseState.Error;
            DoneCallback(true);
        }
    }
}
