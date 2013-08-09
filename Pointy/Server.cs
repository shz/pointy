using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.IO;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Pointy
{
    /// <summary>
    /// The Pointy HTTP Server.
    /// </summary>
    public sealed class Server : IDisposable
    {
        Socket[] Socks;
        int Disposed = 0; // Bool won't work with CompareExchange
        X509Certificate Certificate;
        Core.Scheduler Scheduler;
        volatile IRouter _Router;
        Action<Exception> _ErrorHandler = null;

        Util.SingleLinkedList<Core.Client> ActiveClients = new Util.SingleLinkedList<Core.Client>();
        int AddedClients = 0;

        /// <summary>
        /// Gets or sets the root router used to dispatch requests
        /// </summary>
        public IRouter Router
        {
            get { return _Router; }
            set { _Router = value; }
        }

        public Server()
            : this(0)
        {

        }
        public Server(int threads) : this(null, threads)
        {
        }
        public Server(IRouter router, int? threads = null, X509Certificate certificate = null)
        {
            Certificate = certificate;
            Router = router;

            // Prep the scheduler
            Scheduler = new Core.Scheduler(threads ?? 1);
        }
        ~Server()
        {
            Dispose();
        }
        public void Dispose()
        {
            // Only dispose if it hasn't already happened
            if (0 == Interlocked.CompareExchange(ref Disposed, 1, 0))
            {
                // Clean up the worker threads
                Scheduler.Stop();

                // Destroy the listening sockets
                for (var i = 0; i < this.Socks.Length; i++)
                {
                    try
                    {
                        Socks[i].Close();
                    }
                    catch (Exception err)
                    {
                        // Should probably log this...
                    }
                }

                // Close any open sockets
                foreach (var r in ActiveClients)
                    r.Dispose();
                ActiveClients.Clear();

                // No need to call the destructor again
                GC.SuppressFinalize(this);
            }
        }  

        #region Internals

        /// <summary>
        /// Handles a new client
        /// </summary>
        /// <param name="stream"></param>
        void AddClient(Socket socket, Stream stream)
        {
            // Prep this connection's client
            var protocol = (Certificate == null) ? HTTP.Protocol.HTTP : HTTP.Protocol.HTTPS;
            Core.Client client = null; // Avoids error with client
            client = new Core.Client(socket, stream, protocol, Router, Scheduler.Queue);

            // Add to the active client list so that the client will be killed
            // when the server is shut down.
            lock (ActiveClients)
                ActiveClients.Add(client);

            // Tell the client to start parsing on the socket
            client.Start();

            // Dead clients will quickly collect as clients disconnect and network operations
            // complete.  Over time, diposed sockets will accumulate, crowding the list.
            //
            // To avoid holding on to a bunch of these dead sockets for eternity, we purge
            // disposed references from the list in batches once we've added enough client
            // to cross the watermark.  Not perfect, but it gets the job done.
            if (Interlocked.Increment(ref AddedClients) > 200)
            {
                // Not threadsafe, but the watermark is more of a rough guide
                // anyway, so it doesn't matter.
                AddedClients = 0;

                // Remove those elements
                if (Monitor.TryEnter(ActiveClients))
                {
                    int removed = 0;
                    try
                    {
                        Util.SingleLinkedListNode<Core.Client> last = null;
                        for (var node = ActiveClients.First; node != null; node = node.Next)
                        {
                            // If the reference is dead, remove the node
                            if (!node.Value.IsDisposed)
                            {
                                removed++;
                                if (last != null)
                                    last.Next = node.Next;
                                else
                                    ActiveClients.First = node.Next;
                            }
                            last = node;
                        }
                    }
                    finally
                    {
                        // Console.WriteLine("Reaped {0} dead clients", removed);
                        Monitor.Exit(ActiveClients);
                    }
                }
            }

            return;
        }

        #endregion

        #region Socket Callbacks

        // These are just a bit too big to use as inline delegates
        void AcceptCallback(object sender, SocketAsyncEventArgs args)
        {
            // If there was an error, just bail out now and save ourselves the trouble
            if (args.SocketError != SocketError.Success) return;

            // Disable Nagle algorithm, which works around an issue where
            // we get up to a 200ms delay in some cases (delayed ACK).
            //args.AcceptSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            args.AcceptSocket.NoDelay = true;

            // Create the client
            NetworkStream client = new NetworkStream(args.AcceptSocket, true);

            // Non SSL servers can start handling data right away
            if (Certificate == null)
            {
                // Do the bootstrap
                AddClient(args.AcceptSocket, client);
            }
            // SSL servers need to authenticate first
            else
            {
                SslStream sslClient = new SslStream(client, false);
                sslClient.BeginAuthenticateAsServer(Certificate,
                    false,
                    System.Security.Authentication.SslProtocols.Tls,
                    false,
                    SSLAuthCallback,
                    sslClient);
            }

            // Listen for another connection
            args.AcceptSocket = null;
            var sock = args.UserToken as Socket;
            if (Disposed == 0)
                if (!sock.AcceptAsync(args))
                    AcceptCallback(sock, args);
            
        }
        void SSLAuthCallback(IAsyncResult ar)
        {
            SslStream client = (SslStream)ar.AsyncState;

            // Wrap up the authentication
            client.EndAuthenticateAsServer(ar);

            // And finish with this client
            AddClient(null, client);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Sets the global error handler, which will be called when
        /// an exception is raised inside worker threads.
        /// 
        /// Default behavior is to print the exception to the console.
        /// </summary>
        public Action<Exception> ErrorHandler
        {
            set
            {
                if (Scheduler != null)
                    Scheduler.ErrorHandler = value;
                else
                    _ErrorHandler = value;
            }
        }

        /// <summary>
        /// Starts the Pointy server on the specified endpoint(s)
        /// </summary>
        public void Run(params System.Net.IPEndPoint[] endpoints)
        {
            // Can't restart a disposed server
            if (Disposed == 1) throw new ObjectDisposedException("Server");

            // Fire up the scheduler
            Scheduler.Run();
            if (_ErrorHandler != null)
                Scheduler.ErrorHandler = _ErrorHandler;

            // Fire up the sockets
            Socks = new Socket[endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
            {
                // Setup
                Socks[i] = new Socket(endpoints[i].AddressFamily, SocketType.Stream, ProtocolType.IP);
                Socks[i].Bind(endpoints[i]);

                // Start accepting connections
                Socks[i].Listen(1000);
                var args = new SocketAsyncEventArgs();
                args.UserToken = Socks[i];
                args.Completed += AcceptCallback;
                if (!Socks[i].AcceptAsync(args))
                    AcceptCallback(Socks[i], args);
            }
        }

        #endregion
    }
}
