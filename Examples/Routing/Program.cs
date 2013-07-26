using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pointy;
using Pointy.HTTP;
using Pointy.Util;
using Pointy.Routers;

namespace Routing
{
    class Program
    {
        static volatile bool Running = true;

        static void Main(string[] args)
        {
            using (var server = new Server(URLRouter.Discover()))
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

    [Route(null)]
    class SomeController
    {
        [Route("/index")]
        public async Task Index(Request req, Response res)
        {
            await res.Start();
            await res.Write("SomeController.Index");
            throw new Exception("bad news everyone");
            await res.Finish();
        }

        [Route("/some1", Method = "GET")]
        public async Task Some1(Request req, Response res)
        {
            await res.Start();
            await res.Write("SomeController.Index");
            await res.Finish();
        }

        [Get("/some2")]
        public async Task Some2(Request req, Response res)
        {
            await res.Start();
            await res.Write("SomeController.Index");
            await res.Finish();
        }
    }

    [Route("/other")]
    class SomeOtherController
    {
        [Route(@"/something/{echo \d+}")]
        public async Task Something(Request req, Response res, string echo)
        {
            await res.Start();
            await res.Write("SomeOtherController.Something");
            await res.Write("\n" + echo);
            await res.Finish();
        }
    }

    [Route("/cool")]
    class CoolController
    {
        Request Req;
        Response Res;

        public CoolController(Request req, Response res)
        {
            Req = req;
            Res = res;
        }

        [Route("/beans")]
        public async Task Beans()
        {
            await Res.Start();
            await Res.Write("True story");
            await Res.Finish();
        }
    }

    [Route("/magic")]
    class MagicController
    {
        public Request Req;
        public Response Res;

        [Route("/ermahgerd/{number}")]
        public async Task Ermahgerd(int number)
        {
            await Res.Start(200);
            await Res.Write(number.ToString());
            await Res.Finish();
        }
    }
}
