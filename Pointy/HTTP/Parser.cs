using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace Pointy.HTTP
{
    /// <summary>
    /// Pointy's HTTP Parser
    /// </summary>
    internal class Parser
    {
        public delegate void ParseErrorDelegate(Error error, string extra);
        public delegate void RequestLineDelegate(string method, string path, HTTP.Version version);
        public delegate void HeaderDelegate(string name, string value);
        public delegate void EndHeadersDelegate();
        public delegate void BodyDelegate(ArraySegment<byte> data);
        public delegate void BodyEndDelegate();
        public delegate void EndDelegate();

        public enum Error
        {
            BadRequest,
            UriTooLong,
            HeaderFieldsTooLarge
        }
        enum EntityType
        {
            None,
            Identity,
            Chunked
        }
        enum ParseStage
        {
            Start,

            RequestLine_StartMethod,
            RequestLine_Method,
            RequestLine_StartURI,
            RequestLine_URI,
            RequestLine_EndURI,
            RequestLine_StartVersion,
            RequestLine_H,
            RequestLine_HT,
            RequestLine_HTT,
            RequestLine_HTTP,
            RequestLine_VersionMajor,
            RequestLine_VersionDot,
            RequestLine_VersionMinor,
            RequestLine_EndVersion,

            Headers_Start,
            Header_NewLine,
            Header_AlmostNewLine,
            Header_Name,
            Header_ValueStart,
            Header_ValueAlmostEnd,
            Header_Value,
            Headers_AlmostEnd,

            EntityChunked_StartChunk,
            EntityChunked_ChunkSize,
            EntityChunked_ChunkSizeCR,
            EntityChunked_Chunk,
            EntityChunked_ChunkEnd,
            EntityChunked_ChunkCR,

            EntityFlat,

            Done,

            Error
        }

        public event ParseErrorDelegate OnParseError;
        public event RequestLineDelegate OnRequestLine;
        public event HeaderDelegate OnHeader;
        public event EndHeadersDelegate OnEndHeaders;
        public event BodyDelegate OnBody;
        public event BodyEndDelegate OnEndBody;
        public event EndDelegate OnEnd;

        /// <summary>
        /// Parser machine state
        /// </summary>
        ParseStage Stage = ParseStage.Start;

        // General purpose parsing state
        int Size1 = 0;
        StringBuilder SB1 = new StringBuilder();
        string Str1;

        // Request line stuff
        HTTP.Version Version;
        
        // Headers stuff
        bool InTrailers = false;
        
        // Entity stuff
        EntityType Entity = EntityType.None;
        int EntitySize = 0;

        public Parser()
        {
        }

        void EmitParseError(Error error, string extra = null)
        {
            var e = OnParseError;
            if (e != null) e(error, extra);
            Stage = ParseStage.Error;
        }
        void EmitRequestLine(string method, string path, HTTP.Version version)
        {
            var e = OnRequestLine;
            if (e != null) e(method, path, version);
        }
        void EmitHeader(string name, string value)
        {
            // Check for a body
            if (Entity == EntityType.None)
            {
                if (name.ToLower() == "transfer-encoding")
                {
                    if (value == "identity")
                        Entity = EntityType.Identity;
                    // Anything not identity is chunked
                    else
                        Entity = EntityType.Chunked;
                }
                else if (name.ToLower() == "content-length")
                {
                    Entity = EntityType.Identity;
                    if (!Int32.TryParse(value, out EntitySize))
                    {
                        EmitParseError(Error.BadRequest);
                        Entity = EntityType.None;
                    }
                }
            }

            // Fire the event
            var e = OnHeader;
            if (e != null) e(name, value);
        }
        void EmitEndHeaders()
        {
            var e = OnEndHeaders;
            if (e != null) e();
        }
        void EmitBody(ArraySegment<byte> data)
        {
            var e = OnBody;
            if (e != null) e(data);
        }
        void EmitBodyEnd()
        {
            var e = OnEndBody;
            if (e != null) e();
        }
        void EmitEnd()
        {
            // Clean up state
            Size1 = 0;
            SB1.Clear();
            Str1 = null;

            // Fire the event
            var e = OnEnd;
            if (e != null) e();
        }

        public void AddBytes(ArraySegment<byte> data)
        {
            for (int i=0; i<data.Count; i++)
            {
                // Get the current byte from the input.  This is here for DRY; you'll
                // see `b` get used a whole lot in the state machine.
                byte b = data.Array[i + data.Offset];

                // This is one hell of a monolithic parser.  You've been warned.
                switch (Stage)
                {
                    #region Error State

                    // If we had a parse error, we're done.  We'll
                    // just silently eat all bytes passed to us.
                    case ParseStage.Error:
                        return;

                    #endregion

                    #region Eat Leading CRs/LFs

                    // I guess this isn't strictly compliant with the
                    // RFC, but we're going to eat leading CRs/LFs so
                    // that telnet users or whatever can be trigger-happy
                    // with the newlines.
                    //
                    // And there was much rejoicing.

                    case ParseStage.Start:
                        if (b == 0x0D || b == 0x0A) //CR, LF
                            break;
                        else
                            goto case ParseStage.RequestLine_StartMethod;

                    #endregion

                    #region Method Parsing

                    case ParseStage.RequestLine_StartMethod:
                        SB1.Clear();
                        Stage = ParseStage.RequestLine_Method;
                        goto case ParseStage.RequestLine_Method;
                    case ParseStage.RequestLine_Method:
                        if (b == 0x20) //Space
                        {
                            Str1 = SB1.ToString();
                            Stage = ParseStage.RequestLine_StartURI;
                        }
                        else if (b < 0x41 || b > 0x5A) // 'A' <= b <= 'Z'
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        else
                        {
                            SB1.Append((char)b);
                        }
                        break;

                    #endregion

                    #region Request URI Parsing

                    case ParseStage.RequestLine_StartURI:
                        if (b == 0x20 || b == 0x09 || b == 0x0D || b == 0x0A) //Space, horizontal tab, CR, LF
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        else
                        {
                            SB1.Clear();
                            SB1.Append((char)b);
                        }
                        Stage = ParseStage.RequestLine_URI;
                        break;

                    case ParseStage.RequestLine_URI:
                        if (b == 0x20) //Space
                        {
                            Stage = ParseStage.RequestLine_StartVersion;
                        }
                        else if (b == 0x09 || b == 0x0D || b == 0x0A) //Horizontal tab, CR, LF
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        else
                        {
                            SB1.Append((char)b);

                            // Make sure the URI isn't ridiculously long.  This also serves
                            // as a mechanism for ensuring a malformed request won't cause
                            // the parser to endlessly consume additional data from the client.
                            //
                            // I'm using another variable rather than the StringBuilder's length
                            // because I figure 4 bytes is worth the tiny (if anything) performance
                            // increase.  Probably should have done some profiling before doing this...
                            if (++Size1 > 4096)
                            {
                                EmitParseError(Error.UriTooLong);
                                return;
                            }
                        }
                        break;

                    #endregion

                    #region HTTP Version Parsing

                    case ParseStage.RequestLine_StartVersion:
                        if (b == 0x48) //H
                        {
                            Stage = ParseStage.RequestLine_H;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_H:
                        if (b == 0x54) //T
                        {
                            Stage = ParseStage.RequestLine_HT;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_HT:
                        if (b == 0x54) //T
                        {
                            Stage = ParseStage.RequestLine_HTT;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_HTT:
                        if (b == 0x50) //P
                        {
                            Stage = ParseStage.RequestLine_HTTP;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_HTTP:
                        if (b == 0x2F) // /
                        {
                            Stage = ParseStage.RequestLine_VersionMajor;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_VersionMajor:
                        if (b == 0x31) //1
                        {
                            Stage = ParseStage.RequestLine_VersionDot;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_VersionDot:
                        if (b == 0x2E) //.
                        {
                            Stage = ParseStage.RequestLine_VersionMinor;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.RequestLine_VersionMinor:
                        Size1 = 0;
                        if (b == 0x31) //1
                        {
                            Version = HTTP.Version.HTTP1_1;
                            Stage = ParseStage.RequestLine_EndVersion;
                        }
                        else if (b == 0x30) //0
                        {
                            Version = HTTP.Version.HTTP1_0;
                            Stage = ParseStage.RequestLine_EndVersion;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;

                    // Look for a CRLF | LF to signal the end of the request line
                    case ParseStage.RequestLine_EndVersion:
                        if (b == 0x0D) // CR
                        {
                            if (Size1 == 0)
                            {
                                Size1++;
                                break;
                            }
                            else
                            {
                                EmitParseError(Error.BadRequest);
                                return;
                            }
                        }
                        else if (b == 0x0A) // LF
                        {
                            EmitRequestLine(Str1, SB1.ToString(), Version);
                            Stage = ParseStage.Headers_Start;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;


                    #endregion

                    #region Header Parsing
                    
                    case ParseStage.Headers_Start:
                        SB1.Clear();
                        if (b == 0x0D) //CR
                        {
                            Stage = ParseStage.Headers_AlmostEnd;
                        }
                        // We allow the headers section to end with just an LF, which
                        // is more flexible than the spec.
                        else if (b == 0x0A)
                        {
                            goto case ParseStage.Headers_AlmostEnd;
                        }
                        else
                        {
                            Stage = ParseStage.Header_Name;
                            goto case ParseStage.Header_Name;
                        }
                        break;
                    case ParseStage.Header_Name:
                        // If we hit the colon, push the stringbuilder's state
                        // into the string, so that we can build the value
                        if (b == 0x3A) // :
                        {
                            // First, make sure there's actually something
                            // in the header name.
                            if (SB1.Length == 0)
                            {
                                EmitParseError(Error.BadRequest);
                                return;
                            }

                            // Store the header name and prepare to parse
                            // the value.
                            Str1 = SB1.ToString();
                            SB1.Clear();
                            Stage = ParseStage.Header_ValueStart;
                        }
                        // Look for invalid characters
                        else if (b > 126 // DEL or Non-ASCII
                             ||  b < 33  // Space or CTL
                             ||  false)  // Theoretically, we could be more precise, but it
                                         // doesn't really seem worth spending time on.
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        else
                        {
                            SB1.Append((char)b);

                            // Limit header name size to a maximum of 128 characters
                            if (SB1.Length > 128)
                            {
                                EmitParseError(Error.HeaderFieldsTooLarge);
                                return;
                            }
                        }
                        break;
                    case ParseStage.Header_ValueStart:
                        if (b != 0x20 && b != 0x09) //Space, HTab
                        {
                            Size1 = 0;
                            Stage = ParseStage.Header_Value;
                            goto case ParseStage.Header_Value;
                        }
                        else
                        {
                            break;
                        }
                        
                    case ParseStage.Header_Value:
                        if (b == 0x0D) //CR
                        {
                            Stage = ParseStage.Header_AlmostNewLine;
                        }
                        // We allow headers to end with LF, which is more flexible
                        // than the spec.
                        else if (b == 0x0A) // LF
                        {
                            goto case ParseStage.Header_AlmostNewLine;
                        }
                        else
                        {
                            SB1.Append((char)b);

                            // Limit header value size to 4096 characters
                            if (SB1.Length > 4096)
                            {
                                EmitParseError(Error.HeaderFieldsTooLarge, Str1);
                                return;
                            }
                        }
                        break;
                    case ParseStage.Header_AlmostNewLine:
                        if (b == 0x0A) //LF
                        {
                            Stage = ParseStage.Header_NewLine;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.Header_NewLine:

                        // Look for the header continuing on the next line.  If the
                        // first character is whitespace, then it's a continuation
                        // and we handle that by adding a ", " to the header's value
                        // and then just appending whatever the value contained in the
                        // header line is.
                        if (b == 0x20 || b == 0x09) //Space, HTab
                        {
                            Stage = ParseStage.Header_Value;
                            SB1.Append(", ");
                        }
                        // If it's anything but whitespace, then the header value is
                        // done, so we can emit it and continue parsing.
                        else
                        {
                            // Emit the header
                            EmitHeader(Str1, SB1.ToString());
                            
                            // Clean up after ourselves
                            SB1.Clear();

                            // Cap the number of headers at 128
                            if (++Size1 > 128)
                            {
                                EmitParseError(Error.HeaderFieldsTooLarge);
                                return;
                            }
                            
                            // If it's a CR, then we're ending headers.
                            if (b == 0x0D) // CR
                            {
                                Stage = ParseStage.Headers_AlmostEnd;
                            }
                            // If it's an LF, then the client just skipped
                            // the CR, and we can handle that gracefully.
                            else if (b == 0x0A)
                            {
                                Stage = ParseStage.Headers_AlmostEnd;
                                goto case ParseStage.Headers_AlmostEnd;
                            }
                            // Otherwise, it's a regular old header and we should
                            // continue parsing it.
                            else
                            {
                                Stage = ParseStage.Header_Name;
                                goto case ParseStage.Header_Name;
                            }
                        }
                        break;
                    case ParseStage.Headers_AlmostEnd:
                        if (b == 0x0A) //LF
                        {
                            if (InTrailers)
                                Stage = ParseStage.Done;
                            else
                                EmitEndHeaders();
                                
                            // Check to see if we should expect a body
                            if (Entity == EntityType.Identity)
                            {
                                // If the size is zero, the body is over
                                if (EntitySize == 0)
                                    Stage = ParseStage.Done;
                                // Otherwise, we've got a flat body
                                else
                                    Stage = ParseStage.EntityFlat;
                            }
                            else if (Entity == EntityType.Chunked)
                            {
                                Stage = ParseStage.EntityChunked_StartChunk;
                            }
                            else
                            {
                                Stage = ParseStage.Done;
                                goto case ParseStage.Done;
                            }
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;


                    #endregion

                    #region Entity Parsing
                    
                    #region Flat
                    case ParseStage.EntityFlat:
                        if (EntitySize > data.Count - i) //there's more data coming in the next buffer
                        {
                            //read to the end of the buffer
                            EmitBody(new ArraySegment<byte>(data.Array, i + data.Offset, data.Count - i));
                            //decrease the remaining byte count
                            EntitySize -= data.Count - i;
                            //force the loop to end
                            i = data.Count;
                        }
                        else //the current buffer contains all the data we need
                        {
                            EmitBody(new ArraySegment<byte>(data.Array, i + data.Offset, EntitySize));
                            i += EntitySize;
                            Stage = ParseStage.Done;
                            goto case ParseStage.Done;
                        }

                        break;
                    #endregion

                    #region Chunked

                    case ParseStage.EntityChunked_StartChunk:
                        SB1.Clear();
                        Stage = ParseStage.EntityChunked_ChunkSize;
                        goto case ParseStage.EntityChunked_ChunkSize;
                    case ParseStage.EntityChunked_ChunkSize:
                        if (b == 0x0D) //CR
                            Stage = ParseStage.EntityChunked_ChunkSizeCR;
                        else if (b == 0x0A) // LF
                            goto case ParseStage.EntityChunked_ChunkSizeCR;
                        else
                            SB1.Append((char)b);
                        break;

                    case ParseStage.EntityChunked_ChunkSizeCR:
                        if (b == 0x0A) //LF
                        {
                            if (!int.TryParse(SB1.ToString().Split(';')[0], System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out Size1))
                            {
                                EmitParseError(Error.BadRequest);
                                return;
                            }

                            // Handle the final chunk (specified by size of 0)
                            if (Size1 == 0)
                            {
                                InTrailers = true;
                                Stage = ParseStage.Headers_Start;
                            }
                            // Carry on
                            else
                            {
                                Stage = ParseStage.EntityChunked_Chunk;
                            }
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;

                    case ParseStage.EntityChunked_Chunk:
                        if (Size1 > data.Count - i) //there's more data coming in the next buffer
                        {
                            //read to the end of the buffer
                            EmitBody(new ArraySegment<byte>(data.Array, i + data.Offset, data.Count - i));
                            //decrease the remaining byte count
                            Size1 -= data.Count - i;
                        }
                        else //the current buffer contains all the data we need
                        {
                            EmitBody(new ArraySegment<byte>(data.Array, i + data.Offset, Size1));
                            i += Size1 - 1;
                            Stage = ParseStage.EntityChunked_ChunkEnd;
                        }
                        break;

                    case ParseStage.EntityChunked_ChunkEnd:
                        if (b == 0x0D) // CR
                        {
                            Stage = ParseStage.EntityChunked_ChunkCR;
                        }
                        else if (b == 0x0A) // LF
                        {
                            goto case ParseStage.EntityChunked_ChunkCR;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;
                    case ParseStage.EntityChunked_ChunkCR:
                        if (b == 0x0A) //LF
                        {
                            Stage = ParseStage.EntityChunked_StartChunk;
                        }
                        else
                        {
                            EmitParseError(Error.BadRequest);
                            return;
                        }
                        break;

                    #endregion

                    #endregion

                    // If we're done, wrap things up and keep on truckin'
                    case ParseStage.Done:

                        // Finish up the request
                        EmitEnd();

                        // Reset some state
                        Entity = EntityType.None;

                        // Go back to the start, which lets us handle more data on this
                        // packet.
                        Stage = ParseStage.Start;
                        break;
                }
            }
        }
    }
}
