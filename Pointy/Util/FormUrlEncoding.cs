﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pointy.Util
{
    /// <summary>
    /// Utility functions for operating on x-www-form-urlencoded data
    /// </summary>
    public class FormUrlEncoding
    {
        /// <summary>
        /// Parses keys/values from x-www-form-urlencoded data into a dictionary.
        /// </summary>
        /// <param name="data">Data in x-www-form-urlencoded form</param>
        /// <returns></returns>
        public static Dictionary<string, string> Parse(string data)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();

            string[] pairs = data.Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                string[] parts = pairs[i].Split('=');
                // If something funky is going on, just skip this entry
                if (parts.Length != 2)
                    continue;
                values[UrlEncoding.Decode(parts[0], true)] = UrlEncoding.Decode(parts[1], true);
            }

            return values;
        }

        public static string Stringify(IDictionary<string, string> data)
        {
            var parts = new List<string>();
            foreach (var kp in data)
                parts.Add(String.Format("{0}={1}", UrlEncoding.Encode(kp.Key), UrlEncoding.Encode(kp.Value)));
            return String.Join("&", parts);
        }
    }
}
