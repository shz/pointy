using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pointy.Util
{
    public class Cookie
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Path { get; set; }
        public string Domain { get; set; }
        public DateTime Expires { get; set; }
        public bool Secure { get; set; }
        public bool HttpOnly { get; set; }

        public override string ToString()
        {
            var cookie = new StringBuilder();
            cookie.AppendFormat("{0}={1};", UrlEncoding.Encode(Name), UrlEncoding.Encode(Value));
            if (Path != null)
                cookie.AppendFormat(" Path={0};", Path);
            if (Domain != null)
                cookie.AppendFormat(" Domain={0};", Domain);
            if (Expires != null)
                cookie.AppendFormat(" Expires={0};", Expires.ToUniversalTime().ToString("R"));
            if (Secure)
                cookie.Append(" Secure;");
            if (HttpOnly)
                cookie.Append(" HttpOnly;");

            return cookie.ToString();
        }

        public void SetOnResponse(HTTP.Response res)
        {
            res.Headers["Set-Cookie"] = this.ToString();
        }
    }

    public class Cookies : Dictionary<string, Cookie>
    {
        /// <summary>
        /// Parses from the format used by a "Cookie" header
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static Cookies Parse(string header)
        {
            var cookies = new Cookies();

            foreach (var h in header.Split(';'))
            {
                var split = h.Split('=');

                // Skip anything that doesn't match our format
                if (split.Length < 2)
                    continue;

                var c = new Cookie()
                {
                    Name = UrlEncoding.Decode(split[0].Trim()),
                    Value = UrlEncoding.Decode(split[1].Trim())
                };
                cookies[c.Name] = c;
            }

            return cookies;
        }

        /// <summary>
        /// Writes cookies to a response using Set-Cookie headers
        /// </summary>
        /// <param name="res"></param>
        public void SetOnResponse(HTTP.Response res)
        {
            foreach (var kp in this)
                kp.Value.SetOnResponse(res);
        }
    }
}
