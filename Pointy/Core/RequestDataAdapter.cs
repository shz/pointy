using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pointy.Core
{
    /// <summary>
    /// Used to pass data to an HTTP.Request object.  Buffers up to one segment
    /// of data internally.
    /// </summary>
    class RequestDataAdapter
    {
        Func<Task> RequestData;
        Queue<ArraySegment<byte>> Buffer = new Queue<ArraySegment<byte>>();

        public RequestDataAdapter(Func<Task> requestData)
        {
            RequestData = requestData;
        }

        public void AddData(ArraySegment<byte> data)
        {
            Buffer.Enqueue(data);
        }
        public async Task<ArraySegment<byte>> Read()
        {

            if (Buffer.Count > 0)
            {
                return Buffer.Dequeue();
            }
            else
            {
                await RequestData();
                return Buffer.Dequeue();
            }
        }
    }
}
