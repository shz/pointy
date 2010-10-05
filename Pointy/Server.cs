// Server.cs
// Main Pointy server logic

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
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.IO;

using Pointy.HTTP;
using Pointy.Util;

namespace Pointy
{
    public delegate void RequestCallback(Request reqeust, Response handler);
    public delegate void FreeSocketCallback(Socket socket);

    /// <summary>
    /// The Pointy HTTP Server.
    /// </summary>
    /// <typeparam name="P">Parser to use</typeparam>
    public sealed class Server<P> : IDisposable where P : IParser, new()
    {
        Socket Sock;
        volatile bool Running;
        IUrlDispatcher Dispatcher;

        IList<Socket> ActiveSockets;

        public Server(IUrlDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            ActiveSockets = new List<Socket>();
        }
        ~Server()
        {
            Dispose();
        }
        public void Dispose()
        {
            //This is probably a bad implementation, but the problem lies in Stop.
            //TODO - In make sure Stop won't throw an exception, and then all is well
            if (Running)
            {
                Running = false;

                //Close any open sockets
                while (ActiveSockets.Count > 0)
                {
                    Socket socket = ActiveSockets[0];
                    FreeSocket(socket);
                    socket.Close();
                }
                ActiveSockets.Clear();

                Sock.Close();
            }
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Starts the Pointy server, using the default port (80 for standard HTTP, 443 if using SSL)
        /// </summary>
        public void Start()
        {
            Start(80);
        }
        /// <summary>
        /// Starts the Pointy server on the specified port.
        /// </summary>
        public void Start(int port)
        {
            //Handle the running state
            if (!Running)
                Running = true;
            else
                throw new Exception("Server is already running");

            //Set up the main socket
            System.Net.IPEndPoint endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, port);
            Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            Sock.Bind(endpoint);

            //Start accepting connections
            Sock.Listen(100);   
            Sock.BeginAccept(new AsyncCallback(AcceptCallback), MakeParser());
        }
        /// <summary>
        /// Stops the Pointy server, closing all active connections
        /// and freeing all resources
        /// </summary>
        public void Stop()
        {
            Dispose();
        }

        /// <summary>
        /// Safely closes a socket
        /// </summary>
        /// <param name="socket">Socket to close</param>
        internal void FreeSocket(Socket socket)
        {
            socket.BeginDisconnect(false, delegate(IAsyncResult result)
            {
                socket.EndDisconnect(result);
            }, null);
        }
        /// <summary>
        /// Closes the socket, removing all internal references
        /// </summary>
        /// <param name="socket"></param>
        internal void RemoveSocket(Socket socket)
        {
            socket.Close();
            lock (ActiveSockets)
                ActiveSockets.Remove(socket);
        }

        /// <summary>
        /// Creates an IParser for use with a client connection
        /// </summary>
        /// <returns></returns>
        IParser MakeParser()
        {
            IParser parser = new P();
            parser.MaximumEntitySize = 1024 * 1024 * 10; //10MB default
            return parser;
        }
        /// <summary>
        /// Adds bytes to a parser and handles any results returned from the parser
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="parser"></param>
        /// <param name="buffer"></param>
        void HandleBytes(Socket socket, IParser parser, ArraySegment<byte> buffer)
        {
            ParseResult result = parser.AddBytes(buffer);

            if (result != null)
            {
                if (result.Response != null)
                {
                    //TODO - write the "info" to the response body
                    socket.Send(Encoding.ASCII.GetBytes(string.Format("HTTP/1.1 {0} {1}\r\n\r\n", result.Response.Code, result.Response.Name)));
                    if (result.Close)
                        FreeSocket(socket);
                }
                else if (result.Request != null)
                {
                    PointyUri uri = result.Request.Uri;
                    RequestCallback callback = Dispatcher.Resolve(ref uri);

                    //update the path, making it relative to the path used to
                    //assign the callback in the dispatcher
                    result.Request.Uri = uri;

                    //execute the callback
                    callback(result.Request, new Response(socket, result.Close, result.Request.Version, this.FreeSocket));
                }
            }
        }

        #region Socket Callbacks

        // These are just a bit too big to use as inline delegates
        void AcceptCallback(IAsyncResult result)
        {
            IParser parser = result.AsyncState as IParser;
            Socket clientSocket;
            try
            {
                clientSocket = Sock.EndAccept(result);
            }
            catch (ObjectDisposedException)
            {
                //Nothing to do
                return;
            }

            //Add the socket to the active socket list
            lock (ActiveSockets)
                ActiveSockets.Add(clientSocket);

            //Prepare the state tuple for the connection
            ConnectionTuple state = new ConnectionTuple(clientSocket, parser, new ArraySegment<byte>(new byte[1024 * 8]));
            
            //Start listening for data on the new socket
            SocketError error = SocketError.Success;
            if (Running)
                clientSocket.BeginReceive(state.Buffer.Array, state.Buffer.Offset, state.Buffer.Count, SocketFlags.None, out error, ReceiveCallback, state);
            
            //Handle errors
            if (error != SocketError.Success)
                FreeSocket(clientSocket);
            
            //Accept another connection
            if (Running)
                    Sock.BeginAccept(AcceptCallback, MakeParser());
            
        }
        void ReceiveCallback(IAsyncResult result)
        {
            ConnectionTuple state = result.AsyncState as ConnectionTuple;

            int bytes = 0;
            SocketError error;
            bytes = state.Socket.EndReceive(result, out error);

            //If there's an error, free the socket
            if (error != SocketError.Success)
            {
                FreeSocket(state.Socket);
            }

            //Check for disconnections
            if (bytes == 0)
            {
                RemoveSocket(state.Socket);
            }
            //handle any data we've been sent, and continue receiving
            else
            {
                //Send bytes to the parser
                ArraySegment<byte> newSegment = new ArraySegment<byte>(state.Buffer.Array, state.Buffer.Offset, bytes);
                HandleBytes(state.Socket, state.Parser, newSegment);

                //The HandleBytes method could potentially have closed our socket
                if (state.Socket.Connected)
                {

                    //Update the buffer
                    if (state.Buffer.Count > bytes)
                        state.Buffer = new ArraySegment<byte>(state.Buffer.Array, state.Buffer.Offset + bytes, state.Buffer.Count - bytes);
                    else
                        state.Buffer = new ArraySegment<byte>(new byte[1024 * 4], 0, 1024 * 4);

                    //Make a new receive call
                    SocketError err = SocketError.Success;
                    if (Running)
                        state.Socket.BeginReceive(state.Buffer.Array, state.Buffer.Offset, state.Buffer.Count, SocketFlags.None, out err, ReceiveCallback, state);

                    //Check for errors
                    if (err != SocketError.Success)
                        FreeSocket(state.Socket);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Utility class for wrapping up a socket, parser, and buffer together
    /// </summary>
    class ConnectionTuple
    {
        //Intentionally not user properties here
        public Socket Socket;
        public IParser Parser;
        public ArraySegment<byte> Buffer;

        public ConnectionTuple(Socket socket, IParser parser, ArraySegment<byte> buffer)
        {
            Socket = socket;
            Parser = parser;
            Buffer = buffer;
        }
        public ConnectionTuple()
            : this(null, null, new ArraySegment<byte>(null))
        {
            
        }
    }
}
