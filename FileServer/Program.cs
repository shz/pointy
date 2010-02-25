// Examples/FileServer/Program.cs
// Simple file server example for Pointy

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
using System.IO;
using System.Net.Mime;

using Pointy;
using Pointy.HTTP;
using Pointy.Parsers;

namespace Hello_World
{

    class FileServer : IUrlDispatcher
    {

        string BasePath;

        public FileServer(string basePath)
        {
            //Make sure the path we're given is valid
            if (!Directory.Exists(basePath))
                throw new Exception("Specified path doesn't exist");
        }

        public RequestCallback Resolve(ref string path)
        {
            string p = path;
            return delegate(Request request, Response response)
            {
                FileAttributes attributes = FileAttributes.Normal;
                string cPath = "";
                try
                {
                    cPath = Path.Combine(BasePath, p);
                    attributes = File.GetAttributes(cPath);
                }
                catch (Exception err)
                {
                    if (err is DirectoryNotFoundException || err is FileNotFoundException)
                    {
                        response.Start(404);
                        response.Finish();
                    }
                    else
                    {
                        //We could probably handle this a bit more nicely
                        response.Start(500);
                        response.Finish();
                    }
                    return;
                }

                //Do a directory listing if the requested path is a directory
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {

                }
                //Otherwise serve the file
                else
                {
                    FileInfo fInfo = new FileInfo(cPath);
                    Stream stream;
                    byte[] buffer = new byte[1024 * 8];

                    try
                    {
                        stream = fInfo.OpenRead(); 
                    }
                    catch (Exception)
                    {
                        response.Start(500);
                        response.Finish();
                        return;
                    }

                    response.Start(200);
                    response.SendHeader("Content-Length", fInfo.Length.ToString());
                    //TODO - Content-Type
                    response.SendHeader("Last-Modified", fInfo.LastWriteTime.ToString("r"));
                    
                    //start the aynsc read operations
                    stream.BeginRead(buffer, 0, buffer.Length, delegate(IAsyncResult result)
                    {
                        int read = stream.EndRead(result);
                        //0 bytes = EOF
                        if (read == 0)
                        {
                            stream.Close();
                            response.Finish();
                        }
                        else
                        {
                            //TODO - write the result to the response, keep reading
                            //response.SendBody(); //TODO - ArraySegment SendBody()
                        }
                    }, null);
                }
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //Check the args
            if (args.Length < 1)
            {
                Console.WriteLine("Must specify folder to serve in command line arguments");
                return;
            }

            using (Server<Powernap> server = new Server<Powernap>(new FileServer(args[0]), 8000))
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
