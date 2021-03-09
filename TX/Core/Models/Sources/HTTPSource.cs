using HeyRed.Mime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;
using TX.Utils;

namespace TX.Core.Models.Sources
{
    public class HttpSource : AbstractSource, ISingleTargetExtracted
    {
        /// <summary>
        /// Initialize the HTTP source with the given URI.
        /// </summary>
        /// <param name="uri">URI with HTTP/HTTPS scheme, unchanged once set.</param>
        public HttpSource(Uri uri) : base(uri)
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
                throw new ArgumentException("Only HTTP or HTTPS scheme supported.");
        }

        public override string ToString() => "HTTP(S) Source: " + Uri.ToString();

        public async Task<AbstractTarget> GetTargetAsync()
        {
            HttpWebRequest req = WebRequest.CreateHttp(Uri);
            req.Method = "HEAD";
            req.KeepAlive = false;
            using(var resp = (HttpWebResponse)await req.GetResponseAsync())
            {
                // abort the request
                if (req != null)
                    req.Abort();
                // get suggested name from url
                string suggestedName = Path.GetFileName(Uri.ToString());
                if(!IsFileNameValid(suggestedName))
                    suggestedName = Path.GetRandomFileName();
                string extension = Path.GetExtension(suggestedName);
                // guess the extension fron content type
                string contentType = (resp.ContentType ?? string.Empty)
                    .Split(';', StringSplitOptions.None)[0];
                if (contentType.Length == 0) contentType = "text/html";
                // don't modify extension if content type is application/octet-stream
                if (!contentType.Equals("application/octet-stream"))
                {
                    if (!MimeTypesMap.GetMimeType(extension).Equals(contentType))
                    {
                        var suggestedExtension = MimeTypesMap.GetExtension(contentType);
                        if (suggestedExtension.Length > 0 &&
                            !Path.GetExtension(suggestedName).Equals(suggestedExtension))
                            suggestedName = suggestedName + "." + suggestedExtension;
                    }
                }

                // check if rangable
                AbstractTarget target = null;
                if (resp.ContentLength <= 0)
                    target = new HttpTarget(Uri, suggestedName);
                else target = new HttpRangableTarget(Uri, suggestedName, resp.ContentLength);
                return target;
            }
        }

        public static bool IsValid(Uri uri) => uri.Scheme.Equals("http") || uri.Scheme.Equals("https");

        private bool IsFileNameValid(string fileName)
        {
            if (fileName == string.Empty) return false;
            return !(Path.GetInvalidFileNameChars().Any(
                (c) => fileName.Contains(c)));
        }
    }
}
