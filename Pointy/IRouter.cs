using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pointy
{
    using Handler = Func<HTTP.Request, HTTP.Response, Task>;

    public interface IRouter
    {
        Handler Resolve(HTTP.Request request);
    }
}
