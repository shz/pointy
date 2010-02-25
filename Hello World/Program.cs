// Examples/Hello World/Program.cs
// Simple "Hello World" example for Pointy

// This is free and unencumbered software released into the public domain.
//
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
//
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.


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
                response.Start();
                response.SendHeader("Content-Length", "12");
                response.SendHeader("Content-Type", "text/plain");
                response.SendBody("Hello World!");
                response.Finish();
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
