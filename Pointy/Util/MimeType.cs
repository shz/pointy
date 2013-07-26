using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Pointy.Util
{
    public class MimeType
    {
        /// <summary>
        /// Map file extension to known MIME-types
        /// </summary>
        static Dictionary<string, MimeType> TypesByExtensions = new Dictionary<string, MimeType>()
        {
            // For the love of all that is good and holy, keep this sorted!
            {"atom", new MimeType("application", "atom+xml")},
            {"css", new MimeType("text", "css")},
            {"csv", new MimeType("text", "csv")},
            {"eot", new MimeType("application", "vnd.ms-fontobject")},
            {"gif", new MimeType("image", "gif")},
            {"htm", new MimeType("text", "html")},
            {"html", new MimeType("text", "html")},
            {"ico", new MimeType("image", "vnd.microsoft.icon")},
            {"jpg", new MimeType("image", "jpeg")},
            {"jpeg", new MimeType("image", "jpeg")},
            {"js", new MimeType("application", "javascript")},
            {"m4a", new MimeType("application", "x-m4a")},
            {"mp3", new MimeType("application", "mp3")},
            {"mp4", new MimeType("application", "mp4")},
            {"pdf", new MimeType("application", "pdf")},
            {"png", new MimeType("image", "png")},
            {"rss", new MimeType("application", "rss+xml")},
            {"rtf", new MimeType("application", "rtf")},
            {"svg", new MimeType("image", "svg+xml")},
            {"tiff", new MimeType("image", "tiff")},
            {"txt", new MimeType("text", "plain")},
            {"woff", new MimeType("application", "font-woff")},
            {"xhtml", new MimeType("application", "xhtml+xml")},
            {"xml", new MimeType("text", "xml")},
            {"zip", new MimeType("application", "zip")},
        };

        /// <summary>
        /// Regex to match MIME header special characters (see RFC2045#5.1)
        /// </summary>
        Regex TSpecialsRE = new Regex("[()<>@,;:\\/\"\\[\\]?=]");

        string _Type;
        string _Subtype;
        IDictionary<string, string> _Parameters;

        public string Type
        {
            get
            {
                return _Type;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                _Type = value;
            }
        }
        public string Subtype
        {
            get
            {
                return _Subtype;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                _Subtype = value;
            }
        }
        public IDictionary<string, string> Parameters
        {
            get
            {
                return _Parameters;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                _Parameters = value;
            }
        }

        public MimeType(string type, string subtype) : this(type, subtype, new Dictionary<string, string>())
        {
            
        }
        public MimeType(string type, string subtype, IDictionary<string, string> parameters)
        {
            Type = type;
            Subtype = subtype;
            Parameters = parameters;
        }

        /// <summary>
        /// Guesses MIME type from file extension
        /// </summary>
        /// <param name="extension">File extension.  May include leading .</param>
        /// <returns></returns>
        public static MimeType ByExtension(string extension)
        {
            // Strip the leading . if it's present
            if (extension.Length > 0 && extension[0] == '.')
                extension = extension.Substring(1);

            MimeType ret = null;
            if (!TypesByExtensions.TryGetValue(extension, out ret))
                ret = new MimeType("application", "octet-stream");
            return ret;
            
        }
        /// <summary>
        /// Parses a MIME type from a string, or null if the string fails to parse
        /// </summary>
        /// <param name="mimetype">MIME type string to parse</param>
        /// <returns></returns>
        public static MimeType Parse(string mimetype)
        {
            // Validate input
            if (mimetype == null)
                throw new ArgumentNullException("mimetype");
            else if (mimetype.Equals(""))
                return null;

            //split out so we can work with parameters
            string[] parts = mimetype.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            //the first item will be the type/subtype
            string[] types = parts[0].Trim().Split(new char[] { '/' }, 2);
            if (types.Length != 2)
                return null;

            //handle parameters
            if (parts.Length > 1)
            {
                Dictionary<string, string> d = new Dictionary<string, string>();

                //parse
                for (int i = 1; i < parts.Length; i++)
                {
                    string[] split = parts[i].Trim().Split(new char[] { '=' }, 2);
                    if (split.Length != 2)
                        return null;
                    d[split[0]] = split[1].Trim('"'); //note the trimming of quotes
                }

                return new MimeType(types[0], types[1], d);
            }
            //or not
            else
            {
                return new MimeType(types[0], types[1]);
            }
        }

        /// <summary>
        /// Converts this MIME type to its string representation.
        /// </summary>
        /// <remarks>
        /// Note that this method will automatically add quotes around parameter values containing
        /// special characters.  For example,
        /// 
        ///     new MimeType("foo", "bar", new Dictionary<string, string>() { {"test", "oh:noes"} }).ToString()
        ///     
        /// returns
        /// 
        ///     foo/bar; test="oh:noes"
        ///     
        /// However, this method will generate a malformed MIME type if the parameter name contains invalid
        /// characters, as well as if the type or subtype contains invalid characters.  It is the user's
        /// responsibilitly to ensure that correctly formatted values are used with this class.
        /// </remarks>
        /// <returns></returns>
        public override string ToString()
        {
            //easy shortcut if there aren't any parameters
            if (Parameters.Count == 0)
            {
                return string.Format("{0}/{1}", Type, Subtype);
            }

            StringBuilder b = new StringBuilder();
            b.Append(Type);
            b.Append('/');
            b.Append(Subtype);

            foreach (KeyValuePair<string, string> pair in Parameters)
            {
                b.Append("; ");
                b.Append(pair.Key);
                b.Append('=');
                if (TSpecialsRE.IsMatch(pair.Value))
                {
                    b.Append('"');
                    b.Append(pair.Value);
                    b.Append('"');
                }
                else
                {
                    b.Append(pair.Value);
                }
            }

            return b.ToString();
        }
    }
}
