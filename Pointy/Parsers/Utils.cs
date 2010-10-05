// Parsers/Utils.cs
// Internal HTTP parser utilities.  Implementors of new parser backends
// may want to use some of the code here for laziness' sake.

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

namespace Pointy.Parsers.Utils
{
    /// <summary>
    /// Some error HTTP responses, used internally by Powernap.
    /// </summary>
    static class ParseError
    {
        public static readonly ParseResponse BadRequest = new ParseResponse(400, "Bad Request");
        public static readonly ParseResponse RequestEntityTooLarge = new ParseResponse(414, "Request Entity Too Large");
        public static readonly ParseResponse RequestURITooLong = new ParseResponse(414, "Request-URI Too Long");
        public static readonly ParseResponse UnsupportedMediaType = new ParseResponse(415, "Unsupported Media Type");
        public static readonly ParseResponse NotImplemented = new ParseResponse(501, "Not Implemented");
        public static readonly ParseResponse HTTPVersionNotSupported = new ParseResponse(505, "HTTP Version Not Supported");
    }

    /// <summary>
    /// Read-only stream implementation using ArraySegments as a backing store.
    /// 
    /// Internally, this class maintains a linked list of ArraySegments and uses
    /// the Stream interface to abstract them away into something nice.
    /// 
    /// </summary>
    /// <example>
    /// byte[] data1 = {0, 1, 2, 3, 4};
    /// byte[] data2 = {5, 6, 7, 8, 9};
    /// UberStream foo = new UberStream();
    /// foo.Append(new ArraySegment&lt;byte&rt;(data1));
    /// foo.Append(new ArraySegment&lt;byte&rt;(data2));
    /// 
    /// byte[] buffer = new byte[10];
    /// foo.Read(buffer, 0, 10);
    /// 
    /// //buffer should now be {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
    /// </example>
    class UberStream : Stream
    {
        LinkedList<ArraySegment<byte>> Segments = new LinkedList<ArraySegment<byte>>();
        LinkedListNode<ArraySegment<byte>> CurrentNode = null;
        long _Length = 0;
        int CurrentOffset = 0;

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                return _Length;
            }
        }
        public override long Position
        {
            get
            {
                LinkedListNode<ArraySegment<byte>> cur = Segments.First;
                long length = CurrentOffset;
                while (cur != CurrentNode)
                {
                    length += cur.Value.Count;
                    cur = cur.Next;
                }
                return length;
            }
            set
            {
                //FIXME - there's probably a better way to describe the exception
                if (CurrentNode == null)
                    throw new IOException("Stream is empty");

                //TODO - handle negative values
                if (value < 0)
                    throw new IOException(); //TODO - better exception here

                LinkedListNode<ArraySegment<byte>> cur = Segments.First;
                long length = 0;
                while (length < value)
                {
                    if (cur.Value.Count + length > value)
                    {
                        CurrentNode = cur;
                        CurrentOffset = (int)(value - length);
                        break;
                    }
                    else
                    {
                        if (cur.Next == null)
                            throw new IOException("Attempted to seek past end of stream");
                        length += cur.Value.Count;
                        cur = cur.Next;
                    }
                }
            }
        }

        // Not Implemented //
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
        public override void Flush()
        {
            throw new NotImplementedException();
        }
        // Implemented //
        public override void Close()
        {
            //flush out the buffer/segment lists
            Segments.Clear();
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            //if empty, return 0 (end of stream)
         	if (CurrentNode == null)
                return 0;

            //advance to the next node, if we're at the end of the current node
            if (CurrentOffset == CurrentNode.Value.Count)
            {
                //if there's no next node, we're at the end of the stream
                if (CurrentNode.Next == null)
                    return 0;

                //advance to the next node
                CurrentNode = CurrentNode.Next;
                CurrentOffset = 0;
                return Read(buffer, offset, count);
            }
            //otherwise, do the reading
            else
            {
                int diff = CurrentNode.Value.Count - CurrentOffset;
                if (count > diff)
                {
                    Buffer.BlockCopy(CurrentNode.Value.Array, CurrentNode.Value.Offset, buffer, offset, diff);
                    CurrentOffset += diff;
                    return diff + Read(buffer, offset + diff, count - diff);
                }
                else
                {
                    Buffer.BlockCopy(CurrentNode.Value.Array, CurrentNode.Value.Offset, buffer, offset, count);
                    CurrentOffset += count;
                    return count;
                }
            }
        }
        public override int ReadByte()
        {
            //if empty, return -1 (end of stream)
            if (CurrentNode == null)
                return -1;

            //advance to the next node if we're at the end of the current node
            if (CurrentOffset == CurrentNode.Value.Count)
            {
                //if there's no next node, we're at the end of the stream
                if (CurrentNode.Next == null)
                    return -1;

                //advance to the next node
                CurrentNode = CurrentNode.Next;
                CurrentOffset = 0;
                return ReadByte();
            }
            else
            {
                return CurrentNode.Value.Array[CurrentNode.Value.Offset + CurrentOffset++];
            }

        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }
            return Position;
        }

        /// <summary>
        /// Appends an ArraySegment to the existing list.
        /// </summary>
        /// <param name="segment"></param>
        public void Append(ArraySegment<byte> segment)
        {
            Segments.AddLast(segment);
            _Length += segment.Count;

            if (CurrentNode == null)
                CurrentNode = Segments.First;
        }
    }
}
