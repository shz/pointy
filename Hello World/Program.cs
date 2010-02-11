using System;
using System.Collections.Generic;
using System.Text;

using Pointy;
using Pointy.HTTP;

namespace Hello_World
{
    class CatchAll : IUrlDispatcher
    {
        public RequestCallback Resolve(ref string path)
        {
            return delegate(Request request, Response response)
            {
                response.StartResponse();
                response.SendHeader("Content-Length", "12");
                response.SendHeader("Content-Type", "text/plain");
                response.SendBody("Hello World!");
                response.FinishResponse();
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (Server<Pointy.Parsers.Powernap> server = new Server<Pointy.Parsers.Powernap>(new CatchAll(), 8000))
            {
                //Start listening for requests
                server.Start();

                Console.WriteLine("Server started");
                Console.WriteLine("Listening on port 8000");
                Console.WriteLine("Press CTRL+C to exit");
                Console.TreatControlCAsInput = true;

                //Run until the user exits
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.C)
                        break;
                }
            }
        }
    }
}
