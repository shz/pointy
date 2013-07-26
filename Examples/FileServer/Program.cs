using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;

using Pointy;
using Pointy.HTTP;
using Pointy.Util;
using Pointy.Routers;

namespace HelloWorld
{

    class FileServer
    {
        /// <summary>
        /// Folder to serve files from
        /// </summary>
        Uri BasePath;

        public FileServer(string basePath)
        {
            // Our URI behavior won't work unless basepath
            // ends with a slash.
            if (!basePath.EndsWith("/"))
                basePath += '/';
            BasePath = new Uri("file://" + basePath);
        }

        public Task Serve(Request req, Response res)
        {
            // Safely combine the two paths; this approach prevents
            // the combined path from getting escaping the base
            // directory.
            Uri fullpath;
            var path = req.Path;
            if (path[0] == '/') path = path.Substring(1); // Ensures we combine correctly
            if (!Uri.TryCreate(BasePath, path, out fullpath))
            {
                // If the URI is invalid for some reason, we'll throw
                // a 400 because that's just whacked out, man.
                return Error(res, 400, "<h1>Bad Request</h1><p>Invalid path.</p>");
            }

            // Load the file's info.  This will trigger any not found or no
            // permissions errors, so we'll handle them here.
            FileAttributes attributes = FileAttributes.Normal;
            try
            {
                attributes = File.GetAttributes(fullpath.AbsolutePath);
            }
            catch (Exception err)
            {
                if (err is ArgumentException || err is NotSupportedException)
                    return Error(res, 400, "<h1>Bad Request</h1><p>Path contains invalid characters.</p>");
                if (err is PathTooLongException)
                    return Error(res, 414, "<h1>Bad Request</h1><p>Path is too long.</p>");
                if (err is FileNotFoundException || err is DirectoryNotFoundException)
                    return Error(res, 404, "<h1>Not Found</h1>");
                if (err is IOException || err is UnauthorizedAccessException)
                    return Error(res, 403, "<h1>Forbidden</h1>");

                // We probably won't get here, because we've handed all documented
                // exceptions the function can raise.  However, for future compatibility
                // we'll keep this here.
                return Error(res, 500, "<h1>Internal Server Error");
            }

            // So we have the file info!  Fantastic.  If it's a directory
            // do a listing, otherwise serve it up.
            Console.WriteLine(fullpath.AbsolutePath);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                return ServeDirectory(res, fullpath.AbsolutePath);
            else
                return ServeFile(res, fullpath.AbsolutePath);
        }

        /// <summary>
        /// Writes an error to the response with the specified code and message
        /// </summary>
        /// <param name="res">Response object to write to</param>
        /// <param name="code">HTTP error code</param>
        /// <param name="message">Message to send</param>
        private async Task Error(Response res, int code, string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            res.Headers["Content-Length"] = message.Length.ToString();
            res.Headers["Content-Type"] = "text/html; charset=utf-8";
            await res.Start(code);
            await res.Write(data);
            await res.Finish();
        }

        /// <summary>
        /// Serves the specified file to the response.  This does not check permissions and
        /// the like -- in fact it does no error checking whatsoever.
        /// </summary>
        /// <param name="res"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        async Task ServeFile(Response res, string path)
        {
            var info = new FileInfo(path);
            res.Headers["Content-Length"] = info.Length.ToString();
            res.Headers["Content-Type"] = MimeType.ByExtension(info.Extension).ToString();
            res.Headers["Last-Modified"] = info.LastWriteTimeUtc.ToString("R");
            await res.Start();
            await res.WriteFile(info);
            await res.Finish();
        }

        /// <summary>
        /// Serves a somewhat-nice HTML directory listing to the response.  Note that
        /// it doesn't check permissions or anything, so it's a potential security
        /// problem.  You've been warned!
        /// </summary>
        /// <param name="res"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        async Task ServeDirectory(Response res, string path)
        {
            var info = new DirectoryInfo(path);
            string[] dirs = Directory.GetDirectories(path);
            string[] files = files = Directory.GetFiles(path);

            // Prep the response
            res.Headers["Content-Type"] = "text/html; charset=utf-8";
            await res.Start();
            await res.Write(String.Format("<!doctype html>\n<html><head><title>Directory Listing: {0}</title></head><body>", path));

            // Write the directories
            await res.Write("<h2>Directories:</h2><ul>");
            foreach (var dir in dirs)
                await res.Write(string.Format("<li><a href=\"{0}/\">{0}</a></li>", dir.Substring(path.Length)));

            // Write the files
            await res.Write("</ul><h2>Files:</h2></ul>");
            foreach (var file in files)
                await res.Write(string.Format("<li><a href=\"{0}/\">{0}</a></li>", file.Substring(path.Length)));
        
            // Wrap up
            await res.Write("</ul></body></html>");
            await res.Finish();
        }
    }

    class Program
    {
        static volatile bool Running = true;

        static void Main(string[] args)
        {
            // The folder we're going to serve from
            string folder = Environment.CurrentDirectory;

            // If an argument is specified, it's the directory we should serve
            if (args.Length > 0)
                folder = Path.Combine(Environment.CurrentDirectory, args[0]);

            // Ensure the folder exists
            DirectoryInfo dir = new DirectoryInfo(folder);
            if (!dir.Exists)
            {
                Console.WriteLine("Cannot serve from {0} because it doesn't exist!", folder);
                Environment.Exit(1);
            }

            // Fire up the server!
            using (var server = new Server(new CatchAllRouter(new FileServer(folder).Serve)))
            {
                //Start listening for requests
                server.Run(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 8888));

                Console.WriteLine("Server started");
                Console.WriteLine("Listening on port 8888");
                Console.WriteLine("Serving from {0}", folder);
                Console.WriteLine("Press CTRL+C to exit");
                Console.WriteLine("");

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
