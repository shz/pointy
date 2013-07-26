using System;
using System.Text;

using Pointy;
using Pointy.Routers;

namespace Hello_World
{
    class Program
    {
        static volatile bool Running = true;

        static void Main(string[] args)
        {
            var router = new CatchAllRouter(async (req, res) =>
            {
                var data = Encoding.UTF8.GetBytes("Hello World");
                res.Headers["Content-Length"] = data.Length.ToString();
                await res.Start(200);
                await res.Write("Hello World");
                await res.Finish();
            });

            using (var server = new Server(router, 1))
            {
                //Start listening for requests
                server.Run(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888));

                Console.WriteLine("Server started");
                Console.WriteLine("Listening on port 8888");
                Console.WriteLine("Press CTRL+C to exit");

                // Quit on CTRL-C
                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    Running = false;
                    Console.WriteLine("Shutting down...");
                };

                //Run until the user exits
                while (Running) System.Threading.Thread.Sleep(100);
            }
        }
    }
}
