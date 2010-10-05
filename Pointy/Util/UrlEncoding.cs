using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Pointy.Util
{
    /// <summary>
    /// URLEncoding utility class
    /// </summary>
    public class UrlEncoding
    {
        /// <summary>
        /// Matches all characters that must be escaped in a URLEncoded string.
        /// </summary>
        /// <remarks>
        /// Note how the \w class isn't being used here.  Unfortunately, it seems
        /// to match characters with accents, which we don't want to happen.
        /// </remarks>
        static Regex EscapeRegex = new Regex(@"[^A-Za-z0-9_\-\.~]", RegexOptions.Compiled);

        /// <summary>
        /// Matches escape sequences and extracts the hex values
        /// </summary>
        static Regex UnescapeRegex = new Regex(@"%([a-fA-F0-9]{2})", RegexOptions.Compiled);

        /// <summary>
        /// Performs URL encoding on a string
        /// </summary>
        /// <param name="str">String to be URL encoded</param>
        /// <returns>URL encoded string</returns>
        public static string Encode(string str)
        {
            return Encode(str, true);
        }
        /// <summary>
        /// Performs URL encoding on a string
        /// </summary>
        /// <param name="str">String to be URL encoded</param>
        /// <param name="oldSpaces">If true, spaces are converted to '+' rather than '%20'</param>
        /// <returns>URL encoded string</returns>
        public static string Encode(string str, bool oldSpaces)
        {
            //do backwards-compatible space handling
            if (oldSpaces)
                str = str.Replace(' ', '+');

            return EscapeRegex.Replace(str, delegate (Match match)
            {
                //replace the matches value with its %-hex equivalent
                byte b = Convert.ToByte(match.Value[0]);
                return String.Format("%{0:x2}", b);
            });
        }

        /// <summary>
        /// Decodes a URL encoded string
        /// </summary>
        /// <param name="str">URL encoded string</param>
        /// <returns>Unencoded string</returns>
        public static string Decode(string str)
        {
            return Decode(str, true);
        }
        /// <summary>
        /// Decodes a URL encoded string
        /// </summary>
        /// <param name="str">URL encoded string</param>
        /// <param name="oldSpaces">If true, '+' characters will be converted to spaces</param>
        /// <returns>Unencoded string</returns>
        public static string Decode(string str, bool oldSpaces)
        {
            //do backwards-compatible space handling
            if (oldSpaces)
                str = str.Replace('+', ' ');

            return UnescapeRegex.Replace(str, delegate(Match match)
            {
                //grab the hex number, parse it to a byte, and cast to a char
                char c = (char)byte.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                return c.ToString();
            });
        }
    }
}
