using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pointy.Routers
{
    using Handler = Func<HTTP.Request, HTTP.Response, Task>;

    /// <summary>
    /// Interfaces to an OWIN application.  Booyah.
    /// </summary>
    public class OWINRouter
    {
        public Handler Resolve(HTTP.Request request)
        {
            throw new NotImplementedException("Working on it!");
        }
    }
}
