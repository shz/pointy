using System;
using System.Collections.Generic;
using System.Text;

using Pointy;

//See RFC2046, RFC2388

namespace Pointy.Util
{
    public class MultipartParser
    {
        enum ParseState
        {
            Preamble,

            Boundary,
            BoundaryTrailingDash1,
            BoundaryTrailingDash2,
            BoundaryTrailingCR,

            Header, //TODO

            Data,

            Epilogue
        }
        
        /// <summary>
        /// Byte contents of the boundary
        /// </summary>
        byte[] Boundary;
        /// <summary>
        /// Index of the next boundary byte we need for boundary parsing
        /// </summary>
        int Bp;
        /// <summary>
        /// Current parsing state
        /// </summary>
        ParseState State;
        /// <summary>
        /// Current parse we're parsing
        /// </summary>
        Multipart.Part Part;

        internal MultipartParser(MimeType mime)
        {
            // Make sure the mimetype is valid
            if (mime == null) throw new ArgumentNullException("mime");
            if (!mime.Type.Equals("multipart")) throw new Exception("Can't parse multipart data because the MIME type isn't multipart");
            if (!mime.Parameters.ContainsKey("boundary")) throw new Exception("Can't parse multipart data because the MIME type doesn't contain a boundary");

            // Grab the boundary and strip wrapping quotes if needed
            string boundary = mime.Parameters["boundary"];
            if (boundary[0] == '"' && boundary[boundary.Length - 1] == '"')
                boundary = boundary.Substring(1, boundary.Length - 2);

            // Initialize our static state
            Boundary = Encoding.ASCII.GetBytes("\r\n--" + boundary);
            Bp = 0;
            State = ParseState.Preamble;

        }

        public void AddBytes(ArraySegment<byte> bytes)
        {
            // This parsing algorithm is a little different.  Because it streams data
            // in we have to be able to do backtracking as we can't tell if we're parsing
            // a multipart boundary or just regular data until we've finished parsing that
            // chunk.  As such, we allocate a small array (in the constructor) that has
            // enough space to hold the boundary.  If it looks like we've got a boundary
            // match, we store the entire boundary in there as we parse it.  If it turns
            // out the data we were storing isn't a boundary, we pop that data from the
            // buffer and emit it as body data.
            //
            // Note that according to the spec, a boundary line can have unlimited
            // whitespace before the line's final CRLF.  This is a problem for us due
            // to the way we allocate that buffer, so we disallow it.  If it ends up
            // being a problem it can be fixed, but we may have to resize the buffer on
            // the fly, which would suck.

            // If we're in the epilogue, there's absolutely no point in parsing
            // anything, so we should just bug out.
            if (State == ParseState.Epilogue)
                return;

            // Loop over the data we've been sent and do the parsing
            for (var i = 0; i < bytes.Count; i++)
            {
                byte c = bytes.Array[bytes.Offset + i];

                switch (State)
                {
                    case ParseState.Preamble:
                        goto case ParseState.Data;

                    case ParseState.Data:
                        
                        // If we're in the boundary, increment the boundary pointer
                        if (c == Boundary[Bp])
                        {
                            // If the Bp
                            if (++Bp > 0)
                                ;
                        }
                        else
                        {
                            // If Bp > 0, then we were previously parsing a boundary
                            // before hitting this miss; we need to emit the contents
                            // of the boundary up to Bp since we previously skipped
                            // writing them to the stream.
                            if (Bp > 0)
                            {

                            }

                            // Write this byte to the stream
                            // TODO - how?
                        }

                        break;

                }
            }
        }
    }

    public class Multipart
    {
        public class Part
        {
            byte[] _Data;
            Dictionary<string, string> _Headers = new Dictionary<string,string>();

            public byte[] Data
            {
                get { throw new NotImplementedException(); }
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

        public static MultipartParser Parse(MimeType mime)
        {
            return new MultipartParser(mime);
        }
    }

    public class MultipartFormData
    {
        // Inheritence is done here via composition rather than the normal approach.  We
        // have an internal vanilla Multipart parser and simply intercept its events and
        // delegate method calls down to it, adding the extra form-data functionality
        // over top. 
        static Multipart Decode(HTTP.Request request)
        {
            throw new NotImplementedException();
        }
    }
}
