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
using Pointy.Util;
using Pointy.Parsers;

//TODO - refactor needed

namespace Hello_World
{

    class FileServer : IUrlDispatcher
    {
        /// <summary>
        /// Folder to serve files from
        /// </summary>
        string BasePath;

        public FileServer(string basePath)
        {
            //Make sure the path we're given is valid
            if (!Directory.Exists(basePath))
                throw new Exception("Specified path doesn't exist");

            BasePath = basePath;
        }

        /// <summary>
        /// Writes an error to the response with the specified code and message
        /// </summary>
        /// <param name="response">Response object to write to</param>
        /// <param name="code">HTTP error code</param>
        /// <param name="message">Message to send</param>
        private void Error(Response response, int code, string message)
        {
            response.Start(code);
            response.SendHeader("Content-Length", string.Format("{0}", message.Length));
            response.SendBody(message);
            response.Finish();
        }

        public RequestCallback Resolve(ref PointyUri uri)
        {
            string p = Pointy.Util.UrlEncoding.Decode(uri.Path, true);
            return delegate(Request request, Response response)
            {
                FileAttributes attributes = FileAttributes.Normal;
                string cPath = "";
                try
                {
                    cPath = Path.Combine(BasePath, p.Substring(1));
                    attributes = File.GetAttributes(cPath);
                    Console.WriteLine(cPath + ": " + p);
                }
                catch (Exception err)
                {
                    if (err is DirectoryNotFoundException || err is FileNotFoundException)
                    {
                        Error(response, 404, "File not found");
                    }
                    else
                    {
                        //This could be handled a bit more nicely, but for the purposes of this
                        //example, calling it a server error works just fine
                        Error(response, 500, "Internal server error");
                    }
                    return;
                }

                //Do a directory listing if the requested path is a directory
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string[] dirs = null; 
                    string[] files = null;

                    try
                    {
                        dirs = Directory.GetDirectories(cPath);
                        files = Directory.GetFiles(cPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //Denied!
                        Error(response, 403, "Access to this folder is forbidden"); //FIXME: Needs a more imposing error message  
                        return;
                    }
                    catch (Exception)
                    {
                        //If it's some other error, we'll just blame it on the server
                        Error(response, 500, "Error performing directory listing");
                        return;
                    }

                    //If we successfully got those lists, we can write them to the client!
                    response.Start();
                    response.SendHeader("Content-Type", "text/html; charset=utf-8"); //Note the proper handling of unicode here
                    response.SendBody(String.Format("<html><head><title>Directory Listing: {0}</title></head><body>", p));

                    //write the directories
                    response.SendBody("<h2>Directories</h2><ul>");
                    foreach (string dir in dirs)
                        //The substring here is an attempt to arrive at a correct relative path.  This is not trivial to do
                        //in .NET, but the hack is good enough for an example.
                        response.SendBody(string.Format("<li><a href=\"{0}/\">{0}</a></li>", dir.Substring(cPath.Length)));

                    //write the files
                    response.SendBody("</ul><h2>Files</h2><ul>");
                    foreach (string file in files)
                        //The substring here is an attempt to arrive at a correct relative path.  This is not trivial to do
                        //in .NET, but the hack is good enough for an example.
                        response.SendBody(string.Format("<li><a href=\"{0}\">{0}</a></li>", file.Substring(cPath.Length)));

                    //wrap up
                    response.SendBody("</ul></body></html>");
                    response.Finish();

                }
                //Otherwise serve the file
                else
                {
                    FileInfo fInfo = new FileInfo(cPath);
                    byte[] buffer = new byte[1024 * 8];

                    //Write some basic info
                    response.Start(200);
                    response.SendHeader("Content-Length", fInfo.Length.ToString());
                    //Note the lack of unicode detection for plaintext type files
                    response.SendHeader("Content-Type", Pointy.Util.MimeType.ByExtension(fInfo.Extension).ToString());
                    response.SendHeader("Last-Modified", fInfo.LastWriteTime.ToString("r"));

                    //Send the file
                    response.SendFile(fInfo.FullName);

                    //Finish up
                    response.Finish();
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
                Console.WriteLine("Must specify folder to serve from in command line arguments");
                return;
            }

            using (Server<Powernap> server = new Server<Powernap>(new FileServer(args[0])))
            {
                //Start listening for requests
                server.Start(8000);

                Console.Clear();
                Console.WriteLine("Server started");
                Console.WriteLine("  Serving from {0}", args[0]);
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
