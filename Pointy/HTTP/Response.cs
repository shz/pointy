// HTTP/Response.cs
// HTTP response class, responsible for writing data back to the client.

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
using System.Net.Sockets;
using System.IO;

namespace Pointy.HTTP
{
    /// <summary>
    /// Attempted to write data to the response in a manner that doesn't conform to the HTTP standard.
    /// 
    /// This is likely to result from incorrectly ordering ResponseHandler method calls.
    /// </summary>
    public class HttpViolationException : Exception
    {
        public HttpViolationException(string message) : base(message)
        {

        }
    }

    /// <summary>
    /// Class used to write handle HTTP responses.
    /// </summary>
    /// <remarks>
    /// ResponseHandler takes care of writing HTTP responses to clients.  It provides four methods for doing so:
    /// <see cref="StartResponse"/> <see cref="SendHeader"/> <see cref="SendBody"/> <see cref="FinishResponse"/>.
    /// Note that each of these methods blocks on network IO, to ensure proper data ordering and thread safety.
    /// 
    /// Both StartResponse and FinishResponse must be called in correct order to properly send a response.
    /// 
    /// Every call to a ResponseHandler method returns a bool, indicating if the call was successful.  If a method
    /// returns false, it indicated that some exception occurred on the socket that prevented network IO from
    /// successfully completing.  When this happens the underlying socket is cleanly disconnected, and any further
    /// ResponseHandler calls will fail silently, returning false as well.  This allows an application to forego
    /// error handling logic when using ResponseHandler, though for performance reasons error checking is preferred.
    /// 
    /// ResponseHandler sends body data as it supplied, using chunked encoding by default.  If a Content-Length
    /// header is sent, ResponseHandler will automatically use indentity encoding, forgoing the use of chunks.
    /// If a client connects using HTTP/1.0 and no Content-Length header is set, ResponseHandler will raise an
    /// error.
    /// 
    /// ResponseHandler enforces correct response usage according to the HTTP spec (RFC2616), but only on
    /// a high level, such as header/body data ordering; header names/values are not checked, and body
    /// length is not enforced.  Similarly, ResponseHandler will allow trailing headers to be sent when
    /// using chunked transfer-encoding, but does not check the TE header to make sure that the client
    /// will actually accept them.
    /// </remarks>
    /// <example>
    /// ResponseHandler handler;
    /// 
    /// //Code handler set somehow
    /// 
    /// //Basic example
    /// handler.StartResponse();
    /// handler.SendBody("Hello World"); //uses UTF8 encoding by default
    /// handler.FinishResponse();
    /// 
    /// //More advanced usage
    /// handler.StartResponse(200, "OK");
    /// handler.SendBody("Hello World", Encoding.ASCIIEncoding); //sends with ASCII encoding
    /// handler.FinishResponse();
    /// 
    /// //Error handling - breaks early from the request handling process, saving
    /// //processing resources
    /// if (!handler.StartResponse())
    ///     return;
    /// if (!handler.SendBody("Hello world"))
    ///     return;
    /// if (!handler.FinishResponse())
    ///     return;
    /// </example>
    public class Response
    {
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
        }

        /// <summary>
        /// CRLF in binary (ASCII) form
        /// </summary>
        static readonly byte[] CRLF = new byte[2]
        {
            0xD, 0xA
        };

        volatile bool SockError = false;
        ResponseState State = ResponseState.ResponseLine;
        Socket ClientSocket;
        HTTP.Versions Version;
        bool Disconnect;
        int ContentLength = -1;
        bool TransferEncodingHeaderSent = false;
        bool ConnectionHeaderSent = false;
        FreeSocketCallback FreeSocket;

        public Response(Socket socket, bool disconnect, HTTP.Versions version, FreeSocketCallback freeSocket)
        {
            ClientSocket = socket;
            Disconnect = disconnect;
            Version = version;
            FreeSocket = freeSocket;
        }

        /// <summary>
        /// Frees the socket, sets the state to Error, and returns false
        /// </summary>
        /// <returns></returns>
        bool Error()
        {
            SockError = true;
            FreeSocket(ClientSocket);
            return false;
        }

        /// <summary>
        /// DRY; Wraps common Socket.Send functionality
        /// </summary>
        /// <returns></returns>
        void Send(string data)
        {
            //The encoder call may or may not throw an EncoderFallbackException.
            //I don't really care. If someone's giving us non-ASCII data, they deserve it.
            Send(Encoding.ASCII.GetBytes(data));
        }
        /// <summary>
        /// DRY; Wraps common Socket.Send functionality
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        void Send(byte[] data)
        {
            ClientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, delegate(IAsyncResult result)
            {
                SocketError error;
                ClientSocket.EndSend(result, out error);

                if (error != SocketError.Success)
                    Error();

            }, null);
        }

        /// <summary>
        /// Begins sending an HTTP 200 OK response to the client.
        /// </summary>
        public bool StartResponse()
        {
            return StartResponse(200);
        }
        /// <summary>
        /// Begins sending an HTTP response to the client with the specified status code and the default
        /// reason phrase for that code.
        /// </summary>
        /// <param name="code"></param>
        public bool StartResponse(int code)
        {
            //This here switch statement calls StartResponse(int, string)
            //with the code and the default reason phrase.  All codes from
            //RFC2616 are implemented.
            //
            //These phrases are matched to their codes directly in the
            //switch.  Refactor the matching into its own method if this
            //list is needed elsewhere.
            switch (code)
            {
                #region 100s

                case 100:
                    return StartResponse(code, "Continue");
                case 101:
                    return StartResponse(code, "Switching Protocols");

                #endregion
                #region 200s

                case 200:
                    return StartResponse(code, "OK");
                case 201:
                    return StartResponse(code, "Created");
                case 202:
                    return StartResponse(code, "Accepted");
                case 203:
                    return StartResponse(code, "Non-Authoritative Information");
                case 204:
                    return StartResponse(code, "No Content");
                case 205:
                    return StartResponse(code, "Reset Content");
                case 206:
                    return StartResponse(code, "Partial Content");

                #endregion
                #region 300s

                case 300:
                    return StartResponse(code, "Multiple Choices");
                case 301:
                    return StartResponse(code, "Moved Permanently");
                case 302:
                    return StartResponse(code, "Found");
                case 303:
                    return StartResponse(code, "See Other");
                case 304:
                    return StartResponse(code, "Not Modified");
                case 305:
                    return StartResponse(code, "Use Proxy");
                case 307:
                    return StartResponse(code, "Temporarly Redirect");

                #endregion
                #region 400s

                case 400:
                    return StartResponse(code, "Bad Request");
                case 401:
                    return StartResponse(code, "Unauthorized");
                case 402:
                    return StartResponse(code, "Payment Required");
                case 403:
                    return StartResponse(code, "Forbidden");
                case 404:
                    return StartResponse(code, "Not Found");
                case 405:
                    return StartResponse(code, "Method Not Allowed");
                case 406:
                    return StartResponse(code, "Not Acceptable");
                case 407:
                    return StartResponse(code, "Proxy Authentication Required");
                case 408:
                    return StartResponse(code, "Request Time-out");
                case 409:
                    return StartResponse(code, "Conflict");
                case 410:
                    return StartResponse(code, "Gone");
                case 411:
                    return StartResponse(code, "Length Required");
                case 412:
                    return StartResponse(code, "Precondition Failed");
                case 413:
                    return StartResponse(code, "Request Entity Too Large");
                case 414:
                    return StartResponse(code, "Request-URI Too Large");
                case 415:
                    return StartResponse(code, "Unsupported Media Type");
                case 416:
                    return StartResponse(code, "Requested range not satisfiable");
                case 417:
                    return StartResponse(code, "Expectation Failed");

                #endregion
                #region 500s

                case 500:
                    return StartResponse(code, "Internal Server Error");
                case 501:
                    return StartResponse(code, "Not Implemented");
                case 502:
                    return StartResponse(code, "Bad Gateway");
                case 503:
                    return StartResponse(code, "Service Unavailable");
                case 504:
                    return StartResponse(code, "Gateway Time-out");
                case 505:
                    return StartResponse(code, "HTTP Version not supported");

                #endregion

                default:
                    return StartResponse(code, "Hi Mom"); //If someone's using a crazy code without
                                                          //supplying a reason phrase, they deserve it.
            }
        }
        /// <summary>
        /// Begins sending an HTTP response to the client with the specified status code and reason phrase.
        /// </summary>
        /// <param name="code">HTTP status code (See RFC2616 6.1.1)</param>
        /// <param name="reasonPhrase">Reason phrase</param>
        public bool StartResponse(int code, string reasonPhrase)
        {
            //Make sure we're in the right response state
            if (SockError)
                return false;
            else if (State != ResponseState.ResponseLine)
                if (State == ResponseState.Done)
                    throw new HttpViolationException("Response has already been sent to the client");
                else
                    throw new HttpViolationException("Response has already been started");

            //Send the Status-Line (RFC2616 6.1)
            Send(string.Concat("HTTP/", Version == HTTP.Versions.HTTP1_0 ? "1.0 " : "1.1 ", code, " ", reasonPhrase, "\r\n"));

            //We're writing headers next
            State = ResponseState.Headers;

            //Let the caller know that nothing went wrong (well, probably nothing went wrong...)
            return true;
        }

        /// <summary>
        /// Sends an HTTP header to the client.  If streaming mode via HTTP/1.1 is being used (default
        /// unless the Content-Length header has been sent) and part of the body has already been sent,
        /// the header will be sent as a trailer and sending of further body data will be illegal.
        /// </summary>
        /// <param name="header">Header name</param>
        /// <param name="value">Header value</param>
        /// <returns></returns>
        public bool SendHeader(string header, string value)
        {
            //Make sure we're in the right response state
            if (SockError)
                return false;
            else if (State == ResponseState.Done)
                throw new HttpViolationException("Response has already been sent to the client");
            else if (State == ResponseState.ResponseLine)
                throw new HttpViolationException("Response has not been started yet");

            //Handle trailers
            if (State == ResponseState.Body)
            {
                if (ContentLength < 0 && Version == Versions.HTTP1_1)
                {
                    //Send the "final" chunk
                    Send("0\r\n");

                    //Prevent any more body chunks from being sent
                    State = ResponseState.Trailers;
                }
                else
                {
                    throw new HttpViolationException("Trailers only allowed with streaming mode via HTTP/1.1 requests");
                }
            }

            //If the header is Content-Length, then we need to obey that
            if (header.Equals("Content-Length"))
                ContentLength = int.Parse(value); //This could throw an exception, but we want that to happen

            //If the user sends Transfer-Encoding, this affects our streaming
            if (header.Equals("Transfer-Encoding"))
                TransferEncodingHeaderSent = true;

            //We track this for HTTP/1.0 keep alive
            if (header.Equals("Connection"))
                ConnectionHeaderSent = true;

            //Send the header line (Concat appears to be the fastest choice here)
            Send(string.Concat(header, ": ", value, "\r\n"));

            return true;
        }

        /// <summary>
        /// Sends a string to the client, using UTF8 encoding
        /// </summary>
        /// <param name="data">String to send</param>
        /// <returns></returns>
        public bool SendBody(string data)
        {
            return SendBody(Encoding.UTF8.GetBytes(data));
        }
        /// <summary>
        /// Sends a string to the client, using the specified encoding
        /// </summary>
        /// <param name="data">String to send</param>
        /// <param name="encoding">Encoding to use</param>
        /// <returns></returns>
        public bool SendBody(string data, Encoding encoding)
        {
            return SendBody(encoding.GetBytes(data));
        }
        /// <summary>
        /// Sends data to the client.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SendBody(byte[] data)
        {
            //Make sure we're in the right response state
            if (SockError)
                return false;
            else if (State == ResponseState.Done)
                throw new HttpViolationException("Response has already been sent to the client");
            else if (State == ResponseState.ResponseLine)
                throw new HttpViolationException("Response has not been started yet");
            else if (State == ResponseState.Trailers)
                throw new HttpViolationException("Can't send more body data after trailers");

            //If we have no Content-Length...
            if (ContentLength < 0)
            {
                //... and we're using HTTP/1.1, we default to chunked transfer-encoding.
                //If the user hasn't already sent the header, do it for them.  A nice bonus
                //here is that by RFC2616, if Transfer-Encoding is sent it must include
                //chunked, and chunked must be the last encoding applied.  This means that
                //unless the user is breaking the spec, our chunked handling WILL work...
                if (Version == HTTP.Versions.HTTP1_1 && !TransferEncodingHeaderSent)
                {
                    if (!SendHeader("Transfer-Encoding", "chunked"))
                        return false;
                }
                //Streaming with HTTP requires some work
                else if (Version == Versions.HTTP1_0)
                {
                    //Can't support keep alive while streaming
                    Disconnect = true;
                }
            }
            //If we're using HTTP/1.0 keep-alive, we need to add it as a header
            else if (Version == Versions.HTTP1_0 && Disconnect == false)
            {
                //If the user sets the Connection header, we assume no keep-alive
                //FIXME: If the user sends Connection: Keep-Alive, don't disconnect
                if (ConnectionHeaderSent)
                    Disconnect = true;
                else
                    if (!SendHeader("Connection", "Keep-Alive"))
                        return false;
            }

            //If we're just writing the first body piece, write a CRLF to
            //signal the end of the headers (RFC2616 6)
            if (State == ResponseState.Headers)
                Send(CRLF);

            //If we're in chunked mode, send the chunk length
            if (ContentLength < 0 && Version == HTTP.Versions.HTTP1_1)
                Send(string.Format("{0:x}\r\n", data.Length));

            //Write ze data
            Send(data);

            //If we're in chunked mode, send another CRLF to polish off the chunk
            if (ContentLength < 0 && Version == HTTP.Versions.HTTP1_1)
                Send(CRLF);

            //Make sure state is set correctly
            State = ResponseState.Body;

            //And give the caller the all-clear
            return true;
        }
        
        /// <summary>
        /// Finishes sending the response to the client.
        /// </summary>
        /// <returns></returns>
        public bool FinishResponse()
        {
            //Make sure we're in the right response state
            if (SockError)
                return false;
            else if (State == ResponseState.ResponseLine)
                throw new Exception("Response has not been started yet");
            else if (State == ResponseState.Done)
                throw new Exception("Response has already been sent to the client");

            //If we haven't sent a body, signal the end of the headers
            //by sending a CRLF (RFC2616 6)
            if (State == ResponseState.Headers)
                Send(CRLF);

            //If we're using chunked encoding and haven't sent any
            //trailers, we still need to send the last chunk
            if (ContentLength < 0 && State == ResponseState.Body)
                Send("0\r\n");

            //All the cases converge to requiring one more CRLF
            Send(CRLF);

            //Disconnect the socket if we're instructed to
            if (Disconnect)
                FreeSocket(ClientSocket);

            //Update the state to prevent any more writing on this ResponseHandler
            State = ResponseState.Done;

            //Tell the caller that we're good to go
            return true;
        }
    }
}
