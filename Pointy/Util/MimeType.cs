using System;
using System.Collections.Generic;
using System.Text;

namespace Pointy.Util
{
    /// <summary>
    /// TODO - xml docs
    /// </summary>
    public class MimeType
    {
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
            //Strip the leading . if it's present
            if (extension.Length > 0 && extension[0] == '.')
                extension = extension.Substring(1);

            //And now, a massive switch statement.
            //NOTE - If we migrate to 3.0, change this to be a static dictionary
            //       built using a collection initializer.
            //This list built from 
            // - http://www.webmaster-toolkit.com/mime-types.shtml
            //Keep it sorted
            switch (extension)
            {
                case "3dm":      return new MimeType("x-world", "x-3dmf");
                case "3dmf":     return new MimeType("x-world", "x-3dmf");
                case "a":        return new MimeType("application", "octet-stream");
                case "aab":      return new MimeType("application", "x-authorware-bin");
                case "aam":      return new MimeType("application", "x-authorware-map");
                case "aas":      return new MimeType("application", "x-authorware-seg");
                case "abc":      return new MimeType("text", "vnd.abc");
                case "acgi":     return new MimeType("text", "html");
                case "afl":      return new MimeType("video", "animaflex");
                case "ai":       return new MimeType("application", "postscript");
                case "aif":      return new MimeType("audio", "aiff");
                case "aifc":     return new MimeType("audio", "aiff");
                case "aiff":     return new MimeType("audio", "aiff");
                case "aim":      return new MimeType("application", "x-aim");
                case "aip":      return new MimeType("text", "x-audiosoft-intra");
                case "ani":      return new MimeType("application", "x-navi-animation");
                case "aos":      return new MimeType("application", "x-nokia-9000-communicator-add-on-software");
                case "aps":      return new MimeType("application", "mime");
                case "arc":      return new MimeType("application", "octet-stream");
                case "arj":      return new MimeType("application", "arj");
                case "art":      return new MimeType("image", "x-jg");
                case "asf":      return new MimeType("video", "x-ms-asf");
                case "asm":      return new MimeType("text", "x-asm");
                case "asp":      return new MimeType("text", "asp");
                case "asx":      return new MimeType("application", "x-mplayer2");
                case "au":       return new MimeType("audio", "basic");
                case "avi":      return new MimeType("application", "x-troff-msvideo");
                case "avs":      return new MimeType("video", "avs-video");
                case "bcpio":    return new MimeType("application", "x-bcpio");
                case "bin":      return new MimeType("application", "mac-binary");
                case "bm":       return new MimeType("image", "bmp");
                case "bmp":      return new MimeType("image", "bmp");
                case "boo":      return new MimeType("application", "book");
                case "book":     return new MimeType("application", "book");
                case "boz":      return new MimeType("application", "x-bzip2");
                case "bsh":      return new MimeType("application", "x-bsh");
                case "bz":       return new MimeType("application", "x-bzip");
                case "bz2":      return new MimeType("application", "x-bzip2");
                case "c":        return new MimeType("text", "plain");
                case "c++":      return new MimeType("text", "plain");
                case "cat":      return new MimeType("application", "vnd.ms-pki.seccat");
                case "cc":       return new MimeType("text", "plain");
                case "ccad":     return new MimeType("application", "clariscad");
                case "cco":      return new MimeType("application", "x-cocoa");
                case "cdf":      return new MimeType("application", "cdf");
                case "cer":      return new MimeType("application", "pkix-cert");
                case "cha":      return new MimeType("application", "x-chat");
                case "chat":     return new MimeType("application", "x-chat");
                case "class":    return new MimeType("application", "java");
                case "com":      return new MimeType("application", "octet-stream");
                case "conf":     return new MimeType("text", "plain");
                case "cpio":     return new MimeType("application", "x-cpio");
                case "cpp":      return new MimeType("text", "x-c");
                case "cpt":      return new MimeType("application", "mac-compactpro");
                case "crl":      return new MimeType("application", "pkcs-crl");
                case "crt":      return new MimeType("application", "pkix-cert");
                case "csh":      return new MimeType("application", "x-csh");
                case "css":      return new MimeType("application", "x-pointplus");
                case "cxx":      return new MimeType("text", "plain");
                case "dcr":      return new MimeType("application", "x-director");
                case "deepv":    return new MimeType("application", "x-deepv");
                case "def":      return new MimeType("text", "plain");
                case "der":      return new MimeType("application", "x-x509-ca-cert");
                case "dif":      return new MimeType("video", "x-dv");
                case "dir":      return new MimeType("application", "x-director");
                case "dl":       return new MimeType("video", "dl");
                case "doc":      return new MimeType("application", "msword");
                case "dot":      return new MimeType("application", "msword");
                case "dp":       return new MimeType("application", "commonground");
                case "drw":      return new MimeType("application", "drafting");
                case "dump":     return new MimeType("application", "octet-stream");
                case "dv":       return new MimeType("video", "x-dv");
                case "dvi":      return new MimeType("application", "x-dvi");
                case "dwf":      return new MimeType("drawing", "x-dwf");
                case "dwg":      return new MimeType("application", "acad");
                case "dxf":      return new MimeType("application", "dxf");
                case "dxr":      return new MimeType("application", "x-director");
                case "el":       return new MimeType("text", "x-script.elisp");
                case "elc":      return new MimeType("application", "x-bytecode.elisp");
                case "env":      return new MimeType("application", "x-envoy");
                case "eps":      return new MimeType("application", "postscript");
                case "es":       return new MimeType("application", "x-esrehber");
                case "etx":      return new MimeType("text", "x-setext");
                case "evy":      return new MimeType("application", "envoy");
                case "exe":      return new MimeType("application", "octet-stream");
                case "f":        return new MimeType("text", "plain");
                case "f77":      return new MimeType("text", "x-fortran");
                case "f90":      return new MimeType("text", "plain");
                case "fdf":      return new MimeType("application", "vnd.fdf");
                case "fif":      return new MimeType("application", "fractals");
                case "fli":      return new MimeType("video", "fli");
                case "flo":      return new MimeType("image", "florian");
                case "flx":      return new MimeType("text", "vnd.fmi.flexstor");
                case "fmf":      return new MimeType("video", "x-atomic3d-feature");
                case "for":      return new MimeType("text", "plain");
                case "fpx":      return new MimeType("image", "vnd.fpx");
                case "frl":      return new MimeType("application", "freeloader");
                case "funk":     return new MimeType("audio", "make");
                case "g":        return new MimeType("text", "plain");
                case "g3":       return new MimeType("image", "g3fax");
                case "gif":      return new MimeType("image", "gif");
                case "gl":       return new MimeType("video", "gl");
                case "gsd":      return new MimeType("audio", "x-gsm");
                case "gsm":      return new MimeType("audio", "x-gsm");
                case "gsp":      return new MimeType("application", "x-gsp");
                case "gss":      return new MimeType("application", "x-gss");
                case "gtar":     return new MimeType("application", "x-gtar");
                case "gz":       return new MimeType("application", "x-compressed");
                case "gzip":     return new MimeType("application", "x-gzip");
                case "h":        return new MimeType("text", "plain");
                case "hdf":      return new MimeType("application", "x-hdf");
                case "help":     return new MimeType("application", "x-helpfile");
                case "hgl":      return new MimeType("application", "vnd.hp-hpgl");
                case "hh":       return new MimeType("text", "plain");
                case "hlb":      return new MimeType("text", "x-script");
                case "hlp":      return new MimeType("application", "hlp");
                case "hpg":      return new MimeType("application", "vnd.hp-hpgl");
                case "hpgl":     return new MimeType("application", "vnd.hp-hpgl");
                case "hqx":      return new MimeType("application", "binhex");
                case "hta":      return new MimeType("application", "hta");
                case "htc":      return new MimeType("text", "x-component");
                case "htm":      return new MimeType("text", "html");
                case "html":     return new MimeType("text", "html");
                case "htmls":    return new MimeType("text", "html");
                case "htt":      return new MimeType("text", "webviewhtml");
                case "htx ":     return new MimeType("text", "html");
                case "ice ":     return new MimeType("x-conference", "x-cooltalk");
                case "ico":      return new MimeType("image", "x-icon");
                case "idc":      return new MimeType("text", "plain");
                case "ief":      return new MimeType("image", "ief");
                case "iefs":     return new MimeType("image", "ief");
                case "iges":     return new MimeType("application", "iges");
                case "iges ":    return new MimeType("model", "iges");
                case "igs":      return new MimeType("application", "iges");
                case "ima":      return new MimeType("application", "x-ima");
                case "imap":     return new MimeType("application", "x-httpd-imap");
                case "inf ":     return new MimeType("application", "inf");
                case "ins":      return new MimeType("application", "x-internett-signup");
                case "ip ":      return new MimeType("application", "x-ip2");
                case "isu":      return new MimeType("video", "x-isvideo");
                case "it":       return new MimeType("audio", "it");
                case "iv":       return new MimeType("application", "x-inventor");
                case "ivr":      return new MimeType("i-world", "i-vrml");
                case "ivy":      return new MimeType("application", "x-livescreen");
                case "jam ":     return new MimeType("audio", "x-jam");
                case "jav":      return new MimeType("text", "plain");
                case "java":     return new MimeType("text", "plain");
                case "java ":    return new MimeType("text", "x-java-source");
                case "jcm ":     return new MimeType("application", "x-java-commerce");
                case "jfif":     return new MimeType("image", "jpeg");
                case "jfif-tbnl":return new MimeType("image", "jpeg");
                case "jpe":      return new MimeType("image", "jpeg");
                case "jpeg":     return new MimeType("image", "jpeg");
                case "jpg ":     return new MimeType("image", "jpeg");
                case "jps":      return new MimeType("image", "x-jps");
                case "js ":      return new MimeType("application", "x-javascript");
                case "jut":      return new MimeType("image", "jutvision");
                case "kar":      return new MimeType("audio", "midi");
                case "ksh":      return new MimeType("application", "x-ksh");
                case "la ":      return new MimeType("audio", "nspaudio");
                case "lam":      return new MimeType("audio", "x-liveaudio");
                case "latex ":   return new MimeType("application", "x-latex");
                case "lha":      return new MimeType("application", "lha");
                case "lhx":      return new MimeType("application", "octet-stream");
                case "list":     return new MimeType("text", "plain");
                case "lma":      return new MimeType("audio", "nspaudio");
                case "log ":     return new MimeType("text", "plain");
                case "lsp ":     return new MimeType("application", "x-lisp");
                case "lst ":     return new MimeType("text", "plain");
                case "lsx":      return new MimeType("text", "x-la-asf");
                case "ltx":      return new MimeType("application", "x-latex");
                case "lzh":      return new MimeType("application", "octet-stream");
                case "lzx":      return new MimeType("application", "lzx");
                case "m":        return new MimeType("text", "plain");
                case "m1v":      return new MimeType("video", "mpeg");
                case "m2a":      return new MimeType("audio", "mpeg");
                case "m2v":      return new MimeType("video", "mpeg");
                case "m3u ":     return new MimeType("audio", "x-mpequrl");
                case "man":      return new MimeType("application", "x-troff-man");
                case "map":      return new MimeType("application", "x-navimap");
                case "mar":      return new MimeType("text", "plain");
                case "mbd":      return new MimeType("application", "mbedlet");
                case "mc$":      return new MimeType("application", "x-magic-cap-package-1.0");
                case "mcd":      return new MimeType("application", "mcad");
                case "mcf":      return new MimeType("image", "vasa");
                case "mcp":      return new MimeType("application", "netmc");
                case "me ":      return new MimeType("application", "x-troff-me");
                case "mht":      return new MimeType("message", "rfc822");
                case "mhtml":    return new MimeType("message", "rfc822");
                case "mid":      return new MimeType("application", "x-midi");
                case "midi":     return new MimeType("application", "x-midi");
                case "mif":      return new MimeType("application", "x-frame");
                case "mime ":    return new MimeType("message", "rfc822");
                case "mjf":      return new MimeType("audio", "x-vnd.audioexplosion.mjuicemediafile");
                case "mjpg ":    return new MimeType("video", "x-motion-jpeg");
                case "mm":       return new MimeType("application", "base64");
                case "mme":      return new MimeType("application", "base64");
                case "mod":      return new MimeType("audio", "mod");
                case "moov":     return new MimeType("video", "quicktime");
                case "mov":      return new MimeType("video", "quicktime");
                case "movie":    return new MimeType("video", "x-sgi-movie");
                case "mp2":      return new MimeType("audio", "mpeg");
                case "mp3":      return new MimeType("audio", "mpeg3");
                case "mpa":      return new MimeType("audio", "mpeg");
                case "mpc":      return new MimeType("application", "x-project");
                case "mpe":      return new MimeType("video", "mpeg");
                case "mpeg":     return new MimeType("video", "mpeg");
                case "mpg":      return new MimeType("audio", "mpeg");
                case "mpga":     return new MimeType("audio", "mpeg");
                case "mpp":      return new MimeType("application", "vnd.ms-project");
                case "mpt":      return new MimeType("application", "x-project");
                case "mpv":      return new MimeType("application", "x-project");
                case "mpx":      return new MimeType("application", "x-project");
                case "mrc":      return new MimeType("application", "marc");
                case "ms":       return new MimeType("application", "x-troff-ms");
                case "mv":       return new MimeType("video", "x-sgi-movie");
                case "my":       return new MimeType("audio", "make");
                case "mzz":      return new MimeType("application", "x-vnd.audioexplosion.mzz");
                case "nap":      return new MimeType("image", "naplps");
                case "naplps":   return new MimeType("image", "naplps");
                case "nc":       return new MimeType("application", "x-netcdf");
                case "ncm":      return new MimeType("application", "vnd.nokia.configuration-message");
                case "nif":      return new MimeType("image", "x-niff");
                case "niff":     return new MimeType("image", "x-niff");
                case "nix":      return new MimeType("application", "x-mix-transfer");
                case "nsc":      return new MimeType("application", "x-conference");
                case "nvd":      return new MimeType("application", "x-navidoc");
                case "o":        return new MimeType("application", "octet-stream");
                case "oda":      return new MimeType("application", "oda");
                case "omc":      return new MimeType("application", "x-omc");
                case "omcd":     return new MimeType("application", "x-omcdatamaker");
                case "omcr":     return new MimeType("application", "x-omcregerator");
                case "p":        return new MimeType("text", "x-pascal");
                case "p10":      return new MimeType("application", "pkcs10");
                case "p12":      return new MimeType("application", "pkcs-12");
                case "p7a":      return new MimeType("application", "x-pkcs7-signature");
                case "p7c":      return new MimeType("application", "pkcs7-mime");
                case "p7m":      return new MimeType("application", "pkcs7-mime");
                case "p7r":      return new MimeType("application", "x-pkcs7-certreqresp");
                case "p7s":      return new MimeType("application", "pkcs7-signature");
                case "part ":    return new MimeType("application", "pro_eng");
                case "pas":      return new MimeType("text", "pascal");
                case "pbm ":     return new MimeType("image", "x-portable-bitmap");
                case "pcl":      return new MimeType("application", "vnd.hp-pcl");
                case "pct":      return new MimeType("image", "x-pict");
                case "pcx":      return new MimeType("image", "x-pcx");
                case "pdb":      return new MimeType("chemical", "x-pdb");
                case "pdf":      return new MimeType("application", "pdf");
                case "pfunk":    return new MimeType("audio", "make");
                case "pgm":      return new MimeType("image", "x-portable-graymap");
                case "pic":      return new MimeType("image", "pict");
                case "pict":     return new MimeType("image", "pict");
                case "pkg":      return new MimeType("application", "x-newton-compatible-pkg");
                case "pko":      return new MimeType("application", "vnd.ms-pki.pko");
                case "pl":       return new MimeType("text", "plain");
                case "plx":      return new MimeType("application", "x-pixclscript");
                case "pm":       return new MimeType("image", "x-xpixmap");
                case "pm4 ":     return new MimeType("application", "x-pagemaker");
                case "pm5":      return new MimeType("application", "x-pagemaker");
                case "png":      return new MimeType("image", "png");
                case "pnm":      return new MimeType("application", "x-portable-anymap");
                case "pot":      return new MimeType("application", "mspowerpoint");
                case "pov":      return new MimeType("model", "x-pov");
                case "ppa":      return new MimeType("application", "vnd.ms-powerpoint");
                case "ppm":      return new MimeType("image", "x-portable-pixmap");
                case "pps":      return new MimeType("application", "mspowerpoint");
                case "ppt":      return new MimeType("application", "mspowerpoint");
                case "ppz":      return new MimeType("application", "mspowerpoint");
                case "pre":      return new MimeType("application", "x-freelance");
                case "prt":      return new MimeType("application", "pro_eng");
                case "ps":       return new MimeType("application", "postscript");
                case "psd":      return new MimeType("application", "octet-stream");
                case "pvu":      return new MimeType("paleovu", "x-pv");
                case "pwz ":     return new MimeType("application", "vnd.ms-powerpoint");
                case "py ":      return new MimeType("text", "x-script.phyton");
                case "pyc ":     return new MimeType("applicaiton", "x-bytecode.python");
                case "qcp ":     return new MimeType("audio", "vnd.qcelp");
                case "qd3 ":     return new MimeType("x-world", "x-3dmf");
                case "qd3d ":    return new MimeType("x-world", "x-3dmf");
                case "qif":      return new MimeType("image", "x-quicktime");
                case "qt":       return new MimeType("video", "quicktime");
                case "qtc":      return new MimeType("video", "x-qtc");
                case "qti":      return new MimeType("image", "x-quicktime");
                case "qtif":     return new MimeType("image", "x-quicktime");
                case "ra":       return new MimeType("audio", "x-pn-realaudio");
                case "ram":      return new MimeType("audio", "x-pn-realaudio");
                case "ras":      return new MimeType("application", "x-cmu-raster");
                case "rast":     return new MimeType("image", "cmu-raster");
                case "rexx ":    return new MimeType("text", "x-script.rexx");
                case "rf":       return new MimeType("image", "vnd.rn-realflash");
                case "rgb ":     return new MimeType("image", "x-rgb");
                case "rm":       return new MimeType("application", "vnd.rn-realmedia");
                case "rmi":      return new MimeType("audio", "mid");
                case "rmm ":     return new MimeType("audio", "x-pn-realaudio");
                case "rmp":      return new MimeType("audio", "x-pn-realaudio");
                case "rng":      return new MimeType("application", "ringing-tones");
                case "rnx ":     return new MimeType("application", "vnd.rn-realplayer");
                case "roff":     return new MimeType("application", "x-troff");
                case "rp ":      return new MimeType("image", "vnd.rn-realpix");
                case "rpm":      return new MimeType("audio", "x-pn-realaudio-plugin");
                case "rt":       return new MimeType("text", "richtext");
                case "rtf":      return new MimeType("application", "rtf");
                case "rtx":      return new MimeType("application", "rtf");
                case "rv":       return new MimeType("video", "vnd.rn-realvideo");
                case "s":        return new MimeType("text", "x-asm");
                case "s3m ":     return new MimeType("audio", "s3m");
                case "saveme":   return new MimeType("application", "octet-stream");
                case "sbk ":     return new MimeType("application", "x-tbook");
                case "scm":      return new MimeType("application", "x-lotusscreencam");
                case "sdml":     return new MimeType("text", "plain");
                case "sdp ":     return new MimeType("application", "sdp");
                case "sdr":      return new MimeType("application", "sounder");
                case "sea":      return new MimeType("application", "sea");
                case "set":      return new MimeType("application", "set");
                case "sgm ":     return new MimeType("text", "sgml");
                case "sgml":     return new MimeType("text", "sgml");
                case "sh":       return new MimeType("application", "x-bsh");
                case "shar":     return new MimeType("application", "x-bsh");
                case "shtml":    return new MimeType("text", "x-server-parsed-html");
                case "shtml ":   return new MimeType("text", "html");
                case "sid":      return new MimeType("audio", "x-psid");
                case "sit":      return new MimeType("application", "x-sit");
                case "skd":      return new MimeType("application", "x-koan");
                case "skm ":     return new MimeType("application", "x-koan");
                case "skp ":     return new MimeType("application", "x-koan");
                case "skt ":     return new MimeType("application", "x-koan");
                case "sl ":      return new MimeType("application", "x-seelogo");
                case "smi ":     return new MimeType("application", "smil");
                case "smil ":    return new MimeType("application", "smil");
                case "snd":      return new MimeType("audio", "basic");
                case "sol":      return new MimeType("application", "solids");
                case "spc ":     return new MimeType("application", "x-pkcs7-certificates");
                case "spl":      return new MimeType("application", "futuresplash");
                case "spr":      return new MimeType("application", "x-sprite");
                case "sprite ":  return new MimeType("application", "x-sprite");
                case "src":      return new MimeType("application", "x-wais-source");
                case "ssi":      return new MimeType("text", "x-server-parsed-html");
                case "ssm ":     return new MimeType("application", "streamingmedia");
                case "sst":      return new MimeType("application", "vnd.ms-pki.certstore");
                case "step":     return new MimeType("application", "step");
                case "stl":      return new MimeType("application", "sla");
                case "stp":      return new MimeType("application", "step");
                case "sv4cpio":  return new MimeType("application", "x-sv4cpio");
                case "sv4crc":   return new MimeType("application", "x-sv4crc");
                case "svf":      return new MimeType("image", "vnd.dwg");
                case "svr":      return new MimeType("application", "x-world");
                case "swf":      return new MimeType("application", "x-shockwave-flash");
                case "t":        return new MimeType("application", "x-troff");
                case "talk":     return new MimeType("text", "x-speech");
                case "tar":      return new MimeType("application", "x-tar");
                case "tbk":      return new MimeType("application", "toolbook");
                case "tcl":      return new MimeType("application", "x-tcl");
                case "tcsh":     return new MimeType("text", "x-script.tcsh");
                case "tex":      return new MimeType("application", "x-tex");
                case "texi":     return new MimeType("application", "x-texinfo");
                case "texinfo":  return new MimeType("application", "x-texinfo");
                case "text":     return new MimeType("application", "plain");
                case "tgz":      return new MimeType("application", "gnutar");
                case "tif":      return new MimeType("image", "tiff");
                case "tiff":     return new MimeType("image", "tiff");
                case "tr":       return new MimeType("application", "x-troff");
                case "tsi":      return new MimeType("audio", "tsp-audio");
                case "tsp":      return new MimeType("application", "dsptype");
                case "tsv":      return new MimeType("text", "tab-separated-values");
                case "turbot":   return new MimeType("image", "florian");
                case "txt":      return new MimeType("text", "plain");
                case "uil":      return new MimeType("text", "x-uil");
                case "uni":      return new MimeType("text", "uri-list");
                case "unis":     return new MimeType("text", "uri-list");
                case "unv":      return new MimeType("application", "i-deas");
                case "uri":      return new MimeType("text", "uri-list");
                case "uris":     return new MimeType("text", "uri-list");
                case "ustar":    return new MimeType("application", "x-ustar");
                case "uu":       return new MimeType("application", "octet-stream");
                case "uue":      return new MimeType("text", "x-uuencode");
                case "vcd":      return new MimeType("application", "x-cdlink");
                case "vcs":      return new MimeType("text", "x-vcalendar");
                case "vda":      return new MimeType("application", "vda");
                case "vdo":      return new MimeType("video", "vdo");
                case "vew ":     return new MimeType("application", "groupwise");
                case "viv":      return new MimeType("video", "vivo");
                case "vivo":     return new MimeType("video", "vivo");
                case "vmd ":     return new MimeType("application", "vocaltec-media-desc");
                case "vmf":      return new MimeType("application", "vocaltec-media-file");
                case "voc":      return new MimeType("audio", "voc");
                case "vos":      return new MimeType("video", "vosaic");
                case "vox":      return new MimeType("audio", "voxware");
                case "vqe":      return new MimeType("audio", "x-twinvq-plugin");
                case "vqf":      return new MimeType("audio", "x-twinvq");
                case "vql":      return new MimeType("audio", "x-twinvq-plugin");
                case "vrml":     return new MimeType("application", "x-vrml");
                case "vrt":      return new MimeType("x-world", "x-vrt");
                case "vsd":      return new MimeType("application", "x-visio");
                case "vst":      return new MimeType("application", "x-visio");
                case "vsw ":     return new MimeType("application", "x-visio");
                case "w60":      return new MimeType("application", "wordperfect6.0");
                case "w61":      return new MimeType("application", "wordperfect6.1");
                case "w6w":      return new MimeType("application", "msword");
                case "wav":      return new MimeType("audio", "wav");
                case "wb1":      return new MimeType("application", "x-qpro");
                case "wbmp":     return new MimeType("image", "vnd.wap.wbmp");
                case "web":      return new MimeType("application", "vnd.xara");
                case "wiz":      return new MimeType("application", "msword");
                case "wk1":      return new MimeType("application", "x-123");
                case "wmf":      return new MimeType("windows", "metafile");
                case "wml":      return new MimeType("text", "vnd.wap.wml");
                case "wmlc ":    return new MimeType("application", "vnd.wap.wmlc");
                case "wmls":     return new MimeType("text", "vnd.wap.wmlscript");
                case "wmlsc ":   return new MimeType("application", "vnd.wap.wmlscriptc");
                case "word ":    return new MimeType("application", "msword");
                case "wp":       return new MimeType("application", "wordperfect");
                case "wp5":      return new MimeType("application", "wordperfect");
                case "wp6 ":     return new MimeType("application", "wordperfect");
                case "wpd":      return new MimeType("application", "wordperfect");
                case "wq1":      return new MimeType("application", "x-lotus");
                case "wri":      return new MimeType("application", "mswrite");
                case "wrl":      return new MimeType("application", "x-world");
                case "wrz":      return new MimeType("model", "vrml");
                case "wsc":      return new MimeType("text", "scriplet");
                case "wsrc":     return new MimeType("application", "x-wais-source");
                case "wtk ":     return new MimeType("application", "x-wintalk");
                case "x-png":    return new MimeType("image", "png");
                case "xbm":      return new MimeType("image", "x-xbitmap");
                case "xdr":      return new MimeType("video", "x-amt-demorun");
                case "xgz":      return new MimeType("xgl", "drawing");
                case "xif":      return new MimeType("image", "vnd.xiff");
                case "xl":       return new MimeType("application", "excel");
                case "xla":      return new MimeType("application", "excel");
                case "xlb":      return new MimeType("application", "excel");
                case "xlc":      return new MimeType("application", "excel");
                case "xld ":     return new MimeType("application", "excel");
                case "xlk":      return new MimeType("application", "excel");
                case "xll":      return new MimeType("application", "excel");
                case "xlm":      return new MimeType("application", "excel");
                case "xls":      return new MimeType("application", "excel");
                case "xlt":      return new MimeType("application", "excel");
                case "xlv":      return new MimeType("application", "excel");
                case "xlw":      return new MimeType("application", "excel");
                case "xm":       return new MimeType("audio", "xm");
                case "xml":      return new MimeType("application", "xml");
                case "xmz":      return new MimeType("xgl", "movie");
                case "xpix":     return new MimeType("application", "x-vnd.ls-xpix");
                case "xpm":      return new MimeType("image", "x-xpixmap");
                case "xsr":      return new MimeType("video", "x-amt-showrun");
                case "xwd":      return new MimeType("image", "x-xwd");
                case "xyz":      return new MimeType("chemical", "x-pdb");
                case "z":        return new MimeType("application", "x-compress");
                case "zip":      return new MimeType("application", "x-compressed");
                case "zoo":      return new MimeType("application", "octet-stream");
                case "zsh":      return new MimeType("text", "x-script.zsh");
                default:         return new MimeType("text", "plain");
            }
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
                    d[split[0]] = split[1];
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
        /// Note that this method will not automatically add quotes around parameter values containing
        /// special characters.  For example,
        /// 
        ///     new MimeType("foo", "bar", new Dictionary<string, string>() { {"test", "param()"} }).ToString()
        ///     
        /// returns
        /// 
        ///     foo/bar; test=param()
        ///     
        /// Which is an invalid MIME type; the correct parameter should be "\"param()\"", which would generate
        /// 
        ///     foo/bar; test="param()"
        ///     
        /// Similarly, this method will not generate a malformed MIME type if the parameter name contains invalid
        /// characters, as well as if the type or subtype contains invalid characters.  It is the user's responsibilitly
        /// to ensure that correctly formatted values are used with this class.
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
                b.Append(pair.Value);
            }

            return b.ToString();
        }
    }
}
