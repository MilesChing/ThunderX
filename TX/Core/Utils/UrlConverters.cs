using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Utils
{
    static class UrlConverters
    {
        public static Uri DecodeThunderUri(this Uri uri)
        {
            try
            {
                string strUri = uri.OriginalString;
                Ensure.That(
                    uri.Scheme.Equals("thunder"), 
                    "IsThunderProxy"
                ).IsTrue();
                if (!strUri.EndsWith('/')) strUri += '/';
                string t = Encoding.ASCII.GetString(
                    Convert.FromBase64String(
                        strUri.Substring(10, strUri.Length - 11)
                    )
                );
                return new Uri(t.Substring(2, t.Length - 4));
            }
            catch (Exception) 
            { 
                return null; 
            }
        }
    }
}
