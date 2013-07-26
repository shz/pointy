using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Pointy.Util;

namespace Pointy.HTTP
{
    public class Request
    {
        Core.RequestDataAdapter Adapter;

        public HTTP.Protocol Protocol { get; set; }
        public string Method { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Query { get; set; }

        internal Request(Core.RequestDataAdapter adapter)
        {
            Adapter = adapter;
        }

        public async Task<ArraySegment<byte>> Read()
        {
            return await Adapter.Read();
        }
    }
}
 