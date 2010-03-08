// HTTP/Request.cs
// HTTP request class for Pointy.

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
using System.IO;
using System.Text;

namespace Pointy.HTTP
{
    /// <summary>
    /// Pointy HTTP request object
    /// </summary>
    public class Request
    {
        Methods _Method;
        Versions _Version;
        string _Path;
        Dictionary<string, string> _Headers;
        Stream _Entity;

        /// <summary>
        /// Request method
        /// </summary>
        public Methods Method
        {
            get
            {
                return _Method;
            }
        }
        /// <summary>
        /// Request HTTP version
        /// </summary>
        public Versions Version
        {
            get
            {
                return _Version;
            }
        }
        /// <summary>
        /// Request path
        /// NOTE: The type of this property will be changing in the future to a subtype of System.Uri
        /// </summary>
        public string Path
        {
            get
            {
                return _Path;
            }
            set
            {
                _Path = value;
            }
        }
        /// <summary>
        /// HTTP headers
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get
            {
                return _Headers;
            }
        }
        /// <summary>
        /// Request entity
        /// </summary>
        public Stream Entity
        {
            get
            {
                return _Entity;
            }
        }

        /// <summary>
        /// Creates a new HTTP request
        /// </summary>
        /// <param name="method"></param>
        /// <param name="version"></param>
        /// <param name="path"></param>
        /// <param name="headers"></param>
        /// <param name="entity"></param>
        public Request(Methods method, Versions version, string path, Dictionary<string, string> headers, Stream entity)
        {
            _Method = method;
            _Version = version;
            _Path = path;
            _Headers = headers;
            _Entity = entity;
        }
    }
}
