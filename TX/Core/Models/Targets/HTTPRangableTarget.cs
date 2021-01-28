using EnsureThat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Progresses;

namespace TX.Core.Models.Targets
{
    public class HttpRangableTarget : HttpTarget, ILengthSupported, IRangedStreamExtracted
    {
        /// <summary>
        /// Initialize the HTTP rangable target with its URI and suggested name.
        /// </summary>
        /// <param name="uri">URI of the HTTP target.</param>
        /// <param name="suggestedName">Suggested name of the downloaded file.</param>
        /// <param name="dataLength">Length of data to be downloaded.</param>
        public HttpRangableTarget(Uri uri, string suggestedName, long dataLength) 
            : base(uri, suggestedName) 
        {
            Ensure.That(dataLength).IsGt(0);
            DataLength = dataLength;
        }

        /// <summary>
        /// Length of data to be downloaded.
        /// </summary>
        public long DataLength { get; protected set; }

        public long GetDataLength() => DataLength;

        public async Task<Stream> GetRangedStreamAsync(long begin, long end)
        {
            // check args
            Ensure.That(begin).IsInRange(0, DataLength - 1);
            Ensure.That(end).IsInRange(1, DataLength);
            HttpWebRequest req = WebRequest.CreateHttp(Uri);
            req.Method = "GET";
            req.KeepAlive = false;
            req.Headers["range"] = string.Format("bytes={0}-{1}", begin, end - 1);
            HttpWebResponse resp = (HttpWebResponse)(await req.GetResponseAsync());
            //abort the request
            if (req != null) req.Abort();
            return resp.GetResponseStream();
        }
    }
}
