// TODO - error handling.  Check if path is set, and if not set it to the default.
//      - docstrings

// Ragel based parser for absolute or relative URLs.
//
// This is free and unencumbered software released into the public domain.
//
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
//
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

%%{
    machine url_parser;
    
    action start { start = fpc; }
    
    
    action host_end
    {
        _Host = path.Substring(start, fpc-start);
    }
    action port_end
    {   
        temp = path.Substring(start, fpc-start);
        if (!Int32.TryParse(temp, out _Port))
        {
            //TODO - fail somehow
        }
    }
    action path_end { _Path = path.Substring(start, fpc-start); }
    action query_end
    {
        _Query = new Dictionary<string, string>();
        string[] chunks = path.Substring(start, fpc-start).Split('&');
        string[] split = null;
        for (int i=0; i<chunks.Length; i++)
        {
            split = chunks[i].Split('=');
            _Query[split[0]] = split.Length > 1 ? split[1] : null;
        }
    }
    action fragment_end { _Fragment = path.Substring(start, fpc-start); }
    action error { /* TODO */ }

    # Grammar definition, based mostly on RFC2396
    
    mark        = "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")" ;
    reserved    = ";" | "/" | "?" | ":" | "@" | "&" | "=" | "+" | "$" | "," ;
    unreserved  = alnum | mark ;
    escaped    = "%" xdigit xdigit ;
    
    uchar      = unreserved | escaped ;
    pchar      = uchar | ":" | "@" | "&" | "=" | "+" | "$" | "," ;
    uric       = reserved | unreserved | escaped;
    
    query    = uric* >start %query_end;
    fragment = uric* ;
    
    param         = pchar* ;
    segment       = pchar* (";" param)* ;
    path_segments = segment ("/" segment)* ;
    
    reg_name = (unreserved | escaped | "$" | "," | ";" | ":" | "@" | "&" | "=" | "+")+ ;
    
    IPv4address = digit+ "." digit+ "." digit+ "." digit+ ;
    toplabel    = alpha | (alpha (alnum | "-")* alnum) ;
    domainlabel = alnum | (alnum (alnum | "-")* alnum) ;
    hostname    = (domainlabel ".")* toplabel "."? ;
    #TODO - IPv6
    host        = hostname ;
    port        = digit*;
    hostport    = host >start %host_end (":" port >start %port_end)? ;
    
    userinfo = (unreserved | escaped | ";" | ":" | "&" | "=" | "+" | "$" | ",")* ;
    server   = ((userinfo "@")? hostport)? ;
    
    authority = server | reg_name ;
    
    rel_segment = (unreserved | escaped | ";" | "@" | "&" | "=" | "+" | "$" | ",")+ ;
    
    abs_path = ("/" path_segments) >start %path_end ;
    rel_path = rel_segment abs_path? ;
    net_path = "//" authority abs_path? ;
    
    #We only allow http(s) here
    scheme   = ("http" | "https") ;
    
    relativeURL = (net_path | abs_path | rel_path)? ("?" query)? ;
    absoluteURL = scheme ":" (net_path | abs_path) ("?" query)? ;
    URL         = ( absoluteURL | relativeURL)? ( "#" fragment >start %fragment_end )? ;
    
    main := URL $err(error);
    
}%%
using System;
using System.Collections.Generic;

namespace Pointy.Util
{

    public class PointyUri
    {
        #region Properties
        
        string _OriginalPath;
        string _Host;
        int _Port = 80;
        string _Path;
        Dictionary<string, string> _Query;
        string _Fragment;
        
        public string OriginalPath
        {
            get { return _OriginalPath; }
        }
        public string Host
        {
            get { return _Host; }
        }
        public int Port
        {
            get { return _Port; }
        }
        public string Path
        {
            get { return _Path; }
        }
        public Dictionary<string, string> Query
        {
            get { return _Query; }
        }
        public string Fragment
        {
            get { return _Fragment; }
        }
        
        #endregion
        
        #region Ragel Data
        %% write data;
        #endregion
        
        public PointyUri(string path)
        {
            //Ragel stuff
            int cs;
            int p = 0;
            int pe = path.Length;
            int eof = pe;
            char[] data = path.ToCharArray();
            
            //Used for pulling stuff out
            int start = 0;
            string temp = null;
        
            #region Ragel Init
            %% write init;
            #endregion
     
            #region Ragel Exec
            %% write exec;
            #endregion
            
            #region Post-parse Setup
            
            _OriginalPath = path;
            
            #endregion
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PointyUri))
                return false;
            PointyUri other = obj as PointyUri;

            //note that we specifically don't test OriginalPath equality
            if (this.Port != other.Port) return false;
            if (this._Fragment != null) if (!this._Fragment.Equals(other._Fragment)) return false;
            if (this._Host != null) if (!this._Host.Equals(other._Host)) return false;
            if (this._Path != null) if (!this._Path.Equals(other._Path)) return false;
            if (this._Query != null)
            {
                if (this._Query.Count != other._Query.Count)
                    return false;
                foreach (string key in this._Query.Keys)
                    if (!other._Query.ContainsKey(key) || !other._Query[key].Equals(this._Query[key]))
                        return false;

            }

            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
            string res = "";
            //host
            if (_Host != null)
                res += "http://" + _Host;
            //port
            if (_Port != 80)
                res += String.Format(":{0}", _Port);
            //path
            res += Path;
            //query
            bool first = true;
            foreach (KeyValuePair<string, string> pair in _Query)
            {
                res += String.Format("{0}{1}={2}", first ? "?" : "&", pair.Key, pair.Value);
                first = false;
            }
            //fragment
            if (_Fragment != null)
                res += String.Format("#{0}", _Fragment);
            //done!
            return res;
        }
    }
}
