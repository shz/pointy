using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pointy.Routers
{
    using Handler = Func<HTTP.Request, HTTP.Response, Task>;

    /// <summary>
    /// Routes requests based on host name
    /// </summary>
    public class HostRouter : Dictionary<string, IRouter>, IRouter
    {
        public Handler Resolve(HTTP.Request request)
        {
            IRouter router = null;
            // If we have a router for this host, delegate on down to it
            if (this.TryGetValue(request.Host, out router))
                return router.Resolve(request);
            // Otherwise, bail out
            else
                return null;
        }
    }  
}
