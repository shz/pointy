// Powernap.cs
// The Powernap HTTP parser, designed for use in Pointy, though
// definitely usable elsewhere.

// Copyright (c) 2010 Patrick Stein
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;

using Pointy.HTTP;

namespace Pointy.Parsers
{
    /// <summary>
    /// A Fast REST parser
    /// </summary>
    public class Powernap : IParser
    {
        enum ParseStage
        {
            Start,
            StartCR,

            RequestLine_Start,
            RequestLine_G,
            RequestLine_GE,
            RequestLine_P,
            RequestLine_PU,
            RequestLine_PO,
            RequestLine_POS,
            RequestLine_D,
            RequestLine_DE,
            RequestLine_DEL,
            RequestLine_DELE,
            RequestLine_DELET,
            RequestLine__H, //This would overlap with RequestLine_H, which is for parsing the HTTP version.
            RequestLine_HE, //This workaround is good enough for now
            RequestLine_HEA,
            RequestLine_EndMethod,
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
            RequestLine_AlmostEnd,

            Headers_Start,
            Header_NewLine,
            Header_AlmostNewLine,
            Header_Name,
            Header_ValueStart,
            Header_ValueAlmostEnd,
            Header_Value,
            Header_AlmostEnd,

            EntityChunked_StartChunk,
            EntityChunked_ChunkSize,
            EntityChunked_ChunkSizeCR,
            EntityChunked_Chunk,
            EntityChunked_ChunkEnd,
            EntityChunked_ChunkCR,

            EntityFlat,

            Done
        }

        //Decodes ASCII
        static readonly char[] ASCII = new char[]
        {
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06',
            '\a', '\b', '\t', '\n', '\v', '\f', '\r', '\x0E', '\x0F', 
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', 
            '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', 
            '\x1E', '\x1F', ' ', '!', '"', '#', '$', '%', '&', '\'',
            '(', ')', '*', '+', '\x2C', '-', '.', '/', '0', '1', '2',
            '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>',
            '?', '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
            'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b',
            'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '{', '|', '}', '~', '\x7F'
        };
        //Decodes ASCII, converting all uppercase chars to lowercase
        static readonly char[] LowerASCII = new char[]
        {
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06',
            '\a', '\b', '\t', '\n', '\v', '\f', '\r', '\x0E', '\x0F', 
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', 
            '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', 
            '\x1E', '\x1F', ' ', '!', '"', '#', '$', '%', '&', '\'',
            '(', ')', '*', '+', '\x2C', '-', '.', '/', '0', '1', '2',
            '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>',
            '?', '@', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b',
            'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '{', '|', '}', '~', '\x7F'
        };

        /// <summary>
        /// Current parser state
        /// </summary>
        ParseStage Stage = ParseStage.Start;

        
        //Parsing state.  This can probably be squeezed down to conserve
        //a bit more memory, but unless there's a great need for that
        //I'm not going to worry about it right now.
        
        //FIXME - Some of the variable names here are bad
        Methods Method;
        StringBuilder URI = new StringBuilder();
        int URISize = 0;
        StringBuilder HeaderName = new StringBuilder();
        StringBuilder HeaderValue = new StringBuilder();
        Dictionary<string, string> Headers = new Dictionary<string,string>();
        Versions Version;

        bool DoContinue = false;
        bool InTrailers = false;
        
        int ContentLength = 0;
        Utils.UberStream Content = null;
        Stream CompressionSequence = null;
        StringBuilder ChunkSize = new StringBuilder();

        public Powernap()
        {
        }

        int _MaximumEntitySize;
        public int MaximumEntitySize
        {
            get
            {
                return _MaximumEntitySize;
            }
            set
            {
                _MaximumEntitySize = value;
            }
        }

        public ParseResult AddBytes(ArraySegment<byte> data)
        {
            for (int i=0; i<data.Count; i++)
            {
                //get the current byte from the input
                byte b = data.Array[i + data.Offset];

                //This is one hell of a monolithic parser.  You've been warned.
                switch (Stage)
                {
                    #region Eat Leading CRLFs

                    //I guess this isn't strictly compliant with the
                    //RFC, but we're going to eat leading CRLFs so
                    //that telnet users can be trigger-happy with the
                    //newlines.
                    //
                    //And there was much rejoicing.

                    case ParseStage.Start:
                        if (b == 0x0D) //CR
                        {
                            Stage = ParseStage.StartCR;
                            break;
                        }
                        else
                        {
                            goto case ParseStage.RequestLine_Start;
                        }

                    case ParseStage.StartCR:
                        if (b == 0x0A) //LF
                        {
                            Stage = ParseStage.Start;
                            break;
                        }
                        else
                        {
                            return new ParseResult(ParseError.BadRequest);
                        }

                    #endregion

                    #region Method Parsing

                    case ParseStage.RequestLine_Start:
                        switch (b)
                        {
                            case 0x47: //G
                                Stage = ParseStage.RequestLine_G;
                                break;
                            case 0x50: //P
                                Stage = ParseStage.RequestLine_P;
                                break;
                            case 0x44: //D
                                Stage = ParseStage.RequestLine_D;
                                break;
                            case 0x48: //H
                                Stage = ParseStage.RequestLine__H;
                                break;
                            default:
                                return new ParseResult(ParseError.BadRequest);
                        }
                        break;

                    //Handle the remainder of the HEAD method cases
                    case ParseStage.RequestLine__H:
                        if (b == 0x45) //E
                            Stage = ParseStage.RequestLine_HE;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_HE:
                        if (b == 0x41) //A
                            Stage = ParseStage.RequestLine_HEA;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_HEA:
                        if (b == 0x44) //D
                        {
                            Stage = ParseStage.RequestLine_EndMethod;
                            Method = Methods.Head;
                        }
                        else
                        {
                            return new ParseResult(ParseError.NotImplemented);
                        }
                        break;

                    //Handle the remainder of the GET method cases
                    case ParseStage.RequestLine_G:
                        if (b == 0x45) //E
                            Stage = ParseStage.RequestLine_GE;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_GE:
                        if (b == 0x54) //T
                        {
                            Stage = ParseStage.RequestLine_EndMethod;
                            Method = Methods.Get;
                        }
                        else
                        {
                            return new ParseResult(ParseError.NotImplemented);
                        }
                        break;

                    //Handle the remainder of the DELETE method cases
                    case ParseStage.RequestLine_D:
                        if (b == 0x45) //E
                            Stage = ParseStage.RequestLine_DE;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_DE:
                        if (b == 0x4C) //L
                            Stage = ParseStage.RequestLine_DEL;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_DEL:
                        if (b == 0x45) //E
                            Stage = ParseStage.RequestLine_DELE;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_DELE:
                        if (b == 0x54) //T
                            Stage = ParseStage.RequestLine_DELET;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_DELET:
                        if (b == 0x45) //E
                        {
                            Stage = ParseStage.RequestLine_EndMethod;
                            Method = Methods.Delete;
                        }
                        else
                        {
                            return new ParseResult(ParseError.NotImplemented);
                        }
                        break;

                    //Handle the remainder of the P* cases here
                    case ParseStage.RequestLine_P:
                        switch (b)
                        {
                            case 0x55: //U
                                Stage = ParseStage.RequestLine_PU;
                                break;
                            case 0x4F: //O
                                Stage = ParseStage.RequestLine_PO;
                                break;
                            default:
                                return new ParseResult(ParseError.NotImplemented);
                        }
                        break;
                    case ParseStage.RequestLine_PU:
                        if (b == 0x54) //T
                        {
                            Stage = ParseStage.RequestLine_EndMethod;
                            Method = Methods.Put;
                        }
                        else
                        {
                            return new ParseResult(ParseError.NotImplemented);
                        }
                        break;
                    case ParseStage.RequestLine_PO:
                        if (b == 0x53) //S
                            Stage = ParseStage.RequestLine_POS;
                        else
                            return new ParseResult(ParseError.NotImplemented);
                        break;
                    case ParseStage.RequestLine_POS:
                        if (b == 0x54) //T
                        {
                            Stage = ParseStage.RequestLine_EndMethod;
                            Method = Methods.Post;
                        }
                        else
                        {
                            return new ParseResult(ParseError.NotImplemented);
                        }
                        break;

                    //Make sure the method is followed by a space
                    case ParseStage.RequestLine_EndMethod:
                        if (b == 0x20) //Space
                            Stage = ParseStage.RequestLine_StartURI;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;

                    #endregion

                    #region Request URI Parsing

                    case ParseStage.RequestLine_StartURI:
                        if (b == 0x20 || b == 0x09 || b == 0x0D || b == 0x0A) //Space, horizontal tab, CR, LF
                            return new ParseResult(ParseError.BadRequest);
                        else
                            URI.Append(ASCII[b]);
                        Stage = ParseStage.RequestLine_URI;
                        break;

                    case ParseStage.RequestLine_URI:
                        if (b == 0x20) //Space
                        {
                            Stage = ParseStage.RequestLine_StartVersion;
                        }
                        else
                        {
                            URI.Append(ASCII[b]);

                            //Make sure the URI isn't ridiculously long.  This also serves
                            //as a mechanism for ensuring a malformed request won't cause
                            //the parser to endlessly consume additional data from the client.
                            //
                            //I'm using another variable rather than the StringBuilder's length
                            //because I figure 4 bytes is worth the small (if anything) performance
                            //increase.  Probably should have done some profiling before doing this...
                            if (++URISize > 1024) 
                                return new ParseResult(ParseError.RequestURITooLong);
                        }
                        break;

                    #endregion

                    #region HTTP Version Parsing

                    case ParseStage.RequestLine_StartVersion:
                        if (b == 0x48) //H
                            Stage = ParseStage.RequestLine_H;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_H:
                        if (b == 0x54) //T
                            Stage = ParseStage.RequestLine_HT;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_HT:
                        if (b == 0x54) //T
                            Stage = ParseStage.RequestLine_HTT;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_HTT:
                        if (b == 0x50) //P
                            Stage = ParseStage.RequestLine_HTTP;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_HTTP:
                        if (b == 0x2F) // /
                            Stage = ParseStage.RequestLine_VersionMajor;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_VersionMajor:
                        if (b == 0x31) //1
                            Stage = ParseStage.RequestLine_VersionDot;
                        else
                            return new ParseResult(ParseError.HTTPVersionNotSupported);
                        break;
                    case ParseStage.RequestLine_VersionDot:
                        if (b == 0x2E) //.
                            Stage = ParseStage.RequestLine_VersionMinor;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_VersionMinor:
                        if (b == 0x31) //1
                        {
                            Version = Versions.HTTP1_1;
                            Stage = ParseStage.RequestLine_EndVersion;
                        }
                        else if (b == 0x30) //0
                        {
                            Version = Versions.HTTP1_0;
                            Stage = ParseStage.RequestLine_EndVersion;
                        }
                        else
                        {
                            return new ParseResult(ParseError.HTTPVersionNotSupported);
                        }
                        break;

                    //Make sure the version is followed by a CRLF
                    case ParseStage.RequestLine_EndVersion:
                        if (b == 0x0D) //CR
                            Stage = ParseStage.RequestLine_AlmostEnd;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.RequestLine_AlmostEnd:
                        if (b == 0x0A) //LF
                            Stage = ParseStage.Headers_Start;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;

                    #endregion

                    #region Header Parsing
                    
                    case ParseStage.Headers_Start:
                        if (b == 0x0D) //CR
                            Stage = ParseStage.Header_AlmostEnd;
                        else
                            goto case ParseStage.Header_Name;
                        break;
                    case ParseStage.Header_Name:
                        if (b == 0x3A) // :
                            Stage = ParseStage.Header_ValueStart;
                        else
                            HeaderName.Append(ASCII[b]);
                        break;
                    case ParseStage.Header_ValueStart:
                        if (b != 0x20 && b != 0x09) //Space, HTab
                        {
                            Stage = ParseStage.Header_Value;
                            goto case ParseStage.Header_Value;
                        }
                        else
                        {
                            break;
                        }
                        
                    case ParseStage.Header_Value:
                        if (b == 0x0D) //CR
                            Stage = ParseStage.Header_AlmostNewLine;
                        else
                            HeaderValue.Append(ASCII[b]);
                        break;
                    case ParseStage.Header_AlmostNewLine:
                        if (b == 0x0A) //LF
                            Stage = ParseStage.Header_NewLine;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.Header_NewLine:
                        if (b == 0x20 || b == 0x09) //Space, HTab
                        {
                            Stage = ParseStage.Header_ValueStart;
                            HeaderValue.Append(", ");
                        }
                        else
                        {
                            Headers[HeaderName.ToString()] = HeaderValue.ToString();
                            HeaderValue.Remove(0, HeaderValue.Length);
                            HeaderName.Remove(0, HeaderName.Length);
                        }

                        if (b == 0x0D) //CR
                        {
                            Stage = ParseStage.Header_AlmostEnd;
                        }
                        else
                        {
                            HeaderName.Append(ASCII[b]);
                            Stage = ParseStage.Header_Name;
                        }
                        break;
                    case ParseStage.Header_AlmostEnd:
                        if (b == 0x0A) //LF
                        {
                            if (InTrailers)
                            {
                                Stage = ParseStage.Done;
                            }
                            else
                            {
                                //check for expect: 100-continue (RFC2616 8.2.3)
                                if (Headers.ContainsKey("Expect") && Headers["Expect"].Equals("100-Continue"))
                                    DoContinue = true;

                                //check the headers to see if there's a message body to handle
                                //RFC2616 4.2.2 - Any transfer-encoding that's not "identity" is chunked
                                if (Headers.ContainsKey("Transfer-Encoding") && !Headers["Transfer-Encoding"].Equals("identity"))
                                {
                                    //prep to start decoding the chunky goodness
                                    Stage = ParseStage.EntityChunked_StartChunk;
                                    Content = new Utils.UberStream();

                                    //handle decompression
                                    string[] split = Headers["Transfer-Encoding"].Split(',');
                                    for (int j=0; j<split.Length; j++)
                                    {
                                        string type = split[j].Split(';')[0].Trim().ToLower();

                                        //RFC2616 3.6 - chunked must be the last encoding used
                                        if (j == split.Length - 1 && !type.Equals("chunked"))
                                            return new ParseResult(ParseError.BadRequest);

                                        //GZip encoding
                                        if (type.Equals("gzip") || type.Equals("x-gzip"))
                                        {
                                            if (CompressionSequence != null)
                                                //We don't support nested compression.  It's a bad idea anyway...
                                                return new ParseResult(ParseError.NotImplemented);
                                            else
                                                CompressionSequence = new GZipStream(Content, CompressionMode.Decompress);
                                        }
                                        //Deflate
                                        else if (type.Equals("deflate"))
                                        {
                                            if (CompressionSequence != null)
                                                //We don't support nested compression.  It's a bad idea anyway...
                                                return new ParseResult(ParseError.NotImplemented);
                                            else
                                                CompressionSequence = new DeflateStream(Content, CompressionMode.Decompress);
                                        }


                                    }
                                }
                                else if (Headers.ContainsKey("Content-Length"))
                                {
                                    //grab the content length
                                    if (!Int32.TryParse(Headers["Content-Length"], out ContentLength))
                                        return new ParseResult(ParseError.BadRequest);

                                    //make sure it's within bounds
                                    if (ContentLength > MaximumEntitySize || ContentLength < 0)
                                        return new ParseResult(ParseError.RequestEntityTooLarge); //Yeah, or too small...

                                    //create the content stream
                                    Content = new Utils.UberStream();

                                    //set the state
                                    Stage = ParseStage.EntityFlat;
                                }
                                else
                                {
                                    Stage = ParseStage.Done;
                                }
                            }
                        }
                        else
                        {
                            return new ParseResult(ParseError.BadRequest);
                        }
                        break;


                    #endregion

                    #region Entity Parsing
                    
                    #region Flat
                    case ParseStage.EntityFlat:
                        //Read as much as possible from the data array and throw it into
                        //our uberstream

                        if (ContentLength > data.Count - i) //there's more data coming in the next buffer
                        {
                            //read to the end of the buffer
                            Content.Append(new ArraySegment<byte>(data.Array, i + data.Offset, data.Count - i));
                            //decrease the remaining byte count
                            ContentLength -= data.Count - i;
                        }
                        else //the current buffer contains all the data we need
                        {
                            Content.Append(new ArraySegment<byte>(data.Array, i + data.Offset, ContentLength));
                            Stage = ParseStage.Done;
                        }

                        break;
                    #endregion

                    #region Chunked

                    case ParseStage.EntityChunked_StartChunk:
                        ChunkSize = new StringBuilder();
                        Stage = ParseStage.EntityChunked_ChunkSize;
                        goto case ParseStage.EntityChunked_ChunkSize;
                    case ParseStage.EntityChunked_ChunkSize:
                        if (b == 0x0D) //CR
                            Stage = ParseStage.EntityChunked_ChunkSizeCR;
                        else
                            ChunkSize.Append(LowerASCII[b]);
                        break;

                    case ParseStage.EntityChunked_ChunkSizeCR:
                        if (b == 0x0A) //LF
                        {
                            if (!int.TryParse(ChunkSize.ToString().Split(';')[0], System.Globalization.NumberStyles.HexNumber, System.Globalization.NumberFormatInfo.InvariantInfo, out ContentLength))
                                return new ParseResult(ParseError.BadRequest);

                            //Handle the final chunk (specified by size of 0)
                            if (ContentLength == 0)
                            {
                                InTrailers = true;
                                Stage = ParseStage.Headers_Start;
                            }
                            //Make sure we aren't going over the max entity size
                            else if (Content.Length + ContentLength > MaximumEntitySize)
                            {
                                return new ParseResult(ParseError.RequestEntityTooLarge);
                            }
                            //Carry on (my wayward son...)
                            else
                            {
                                Stage = ParseStage.EntityChunked_Chunk;
                            }
                        }
                        else
                        {
                            return new ParseResult(ParseError.BadRequest);
                        }
                        break;

                    case ParseStage.EntityChunked_Chunk:
                        if (ContentLength > data.Count - i) //there's more data coming in the next buffer
                        {
                            //read to the end of the buffer
                            Content.Append(new ArraySegment<byte>(data.Array, i + data.Offset, data.Count - i));
                            //decrease the remaining byte count
                            ContentLength -= data.Count - i;
                        }
                        else //the current buffer contains all the data we need
                        {
                            Content.Append(new ArraySegment<byte>(data.Array, i + data.Offset, ContentLength));
                            i += ContentLength - 1;
                            Stage = ParseStage.EntityChunked_ChunkEnd;
                        }
                        break;

                    case ParseStage.EntityChunked_ChunkEnd:
                        if (b == 0x0D) //CR
                            Stage = ParseStage.EntityChunked_ChunkCR;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;
                    case ParseStage.EntityChunked_ChunkCR:
                        if (b == 0x0A) //LF
                            Stage = ParseStage.EntityChunked_StartChunk;
                        else
                            return new ParseResult(ParseError.BadRequest);
                        break;

                    #endregion

                    #endregion

                    //if we're done, we shouldn't accept any more data from the client
                    case ParseStage.Done:
                        return new ParseResult(ParseError.BadRequest);
                }

                //if we're done a request, do some cleanup
                if (Stage == ParseStage.Done)
                {
                    //build the request
                    Request request = new Request(Method, Version, URI.ToString(), Headers, CompressionSequence == null ? Content : CompressionSequence);
                    bool close = false;

                    //handle HTTP/1.0 auto disconnect
                    if (Version == Versions.HTTP1_0)
                    {
                        if (!Headers.ContainsKey("Connection") || !Headers["Connection"].ToLower().Equals("keep-alive"))
                            close = true;
                    }
                    //handle HTTP1.1 disconnect stuff
                    else
                    {
                        if (Headers.ContainsKey("Connection") && Headers["Connection"].Equals("close"))
                            close = true;
                    }

                    //cleanup
                    URI.Remove(0, URI.Length);
                    URISize = 0;
                    HeaderName.Remove(0, HeaderName.Length);
                    HeaderValue.Remove(0, HeaderValue.Length);
                    Headers = new Dictionary<string, string>();
                    DoContinue = false;
                    InTrailers = false;
                    Stage = ParseStage.Start;
                    ContentLength = 0;
                    Content = null;
                    CompressionSequence = null;

                    //and return the response
                    return new ParseResult(request, close);
                }
            }

            //send a 100-continue response if we need to
            if (DoContinue)
            {
                DoContinue = false;
                ParseResult res = new ParseResult(ParseResponse.Continue);
                res.Close = false;
                return res;
            }

            //nothing to report, cap'n
            return null;
        }
        //600-line functions are a sign of good software engineering!  Oh, wait...
    }
}
