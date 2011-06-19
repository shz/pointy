using System;
using System.Collections.Generic;
using System.Text;

using Pointy;

//See RFC2046, RFC2388

namespace Pointy.Util
{
    public class Multipart
    {
        public class Part
        {
            byte[] _Data;
            Dictionary<string, string> _Headers = new Dictionary<string,string>();

            public byte[] Data
            {
                get;
            }
            public Dictionary<string, string> Headers
            {
                get
                {
                    return _Headers;
                }
            }

            internal Part(byte[] data, Dictionary<string, string> headers)
            {
                _Headers = headers;
                _Data = data;
            }
        }

        Dictionary<string, Part> _Parts = new Dictionary<string, Part>();
        public Dictionary<string, Part> Parts
        {
            get
            {
                return _Parts;
            }
        }

        internal Multipart(Dictionary<string, Part> parts)
        {
            _Parts = parts;
        }

        public static Multipart Parse(HTTP.Request request)
        {
            Dictionary<string, Multipart.Part> parts = new Dictionary<string, Multipart.Part>();

            //if there's no Content-Type header, we can't decode this
            if (!request.Headers.ContainsKey("Content-Type"))
                throw new Exception("Can't parse this request body as multipart, as it's missing a Content-Type header");

            //parse the mimetype
            MimeType mime = MimeType.Parse(request.Headers["Content-Type"]);

            //make sure parsing succeeded, and the mime type is correct
            if (mime == null || mime.Type != "multipart" || !mime.Parameters.ContainsKey("boundary"))
                throw new Exception("Can't parse this request body as multipart, as the Content-Type isn't multipart");

            // Here follows the parsing
            string boundary = "\r\n--" + mime.Parameters["boundary"] + "\r\n";
            string lastBoundary = boundary + "--";
            byte[] boundary_b = Encoding.ASCII.GetBytes(boundary);
            byte[] lastBoundary_b = Encoding.ASCII.GetBytes(lastBoundary);
            int i = 0;

            //parse state
            bool foundContentType = false;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            int dStart = 0, dEnd = 0;
            int iBoundary = 0;

            //parse
            while (i++ < request.Entity.Length)
            {
                //we can cast because we're counting how far in we are, and will never hit
                //stream end by reading bytes
                byte b = (byte)request.Entity.ReadByte();

                //try to match a boundary
                if (b == boundary_b[iBoundary] || b == lastBoundary_b[iBoundary])
                {
                    iBoundary++;
                }
                else
                {
                    //
                }
                


            }

            return new Multipart(parts);
        }
    }

    //TODO - multipart/mixed, for great justice
    public class MultipartFormData
    {
        static Multipart Decode(HTTP.Request request)
        {
            
        }
    }
}
