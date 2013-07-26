using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pointy.Routers
{
    using Handler = Func<HTTP.Request, HTTP.Response, Task>;

    /// <summary>
    /// Routes all requests to a single handler
    /// </summary>
    public class CatchAllRouter : IRouter
    {
        Handler TheHandler;

        public CatchAllRouter(Handler handler)
        {
            TheHandler = handler;
        }
        public Handler Resolve(HTTP.Request request)
        {
            return TheHandler;
        }
    }
}
