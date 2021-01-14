using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Targets
{
    public class HttpTarget : AbstractTarget, IStreamExtracted
    {
        /// <summary>
        /// Initialize the HTTP target with its URI and suggested name.
        /// </summary>
        /// <param name="uri">URI of the HTTP target.</param>
        /// <param name="suggestedName">Suggested name of the downloaded file.</param>
        public HttpTarget(Uri uri, string suggestedName)
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
                throw new ArgumentException("Only HTTP or HTTPS scheme supported.");

            Uri = uri;
            this.suggestedName = suggestedName;
        }

        /// <summary>
        /// URI of the target.
        /// </summary>
        public Uri Uri { get; protected set; }

        private readonly string suggestedName;

        public async Task<Stream> GetStreamAsync()
        {
            HttpWebRequest req = WebRequest.CreateHttp(Uri);
            req.Method = "GET";
            req.KeepAlive = false;
            HttpWebResponse resp = (HttpWebResponse)(await req.GetResponseAsync());
            //abort the request
            if (req != null)
                req.Abort();
            return resp.GetResponseStream();
        }

        protected override string GetSuggestedName() => suggestedName;
    }
}
