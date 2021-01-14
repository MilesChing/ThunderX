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
            DataLength = dataLength;
        }

        /// <summary>
        /// Length of data to be downloaded.
        /// </summary>
        public long DataLength { get; protected set; }

        public long GetDataLength() => DataLength;

        public async Task<Stream> GetRangedStreamAsync(Range<long> range)
        {
            // check args
            if (range.From < 0 || range.To > DataLength)
                throw new ArgumentOutOfRangeException(
                    string.Format("from = {0} , to = {1} is out of range considering data length {2}",
                        range.From, range.To, DataLength));
            HttpWebRequest req = WebRequest.CreateHttp(Uri);
            req.Method = "GET";
            req.KeepAlive = false;
            req.Headers["range"] = string.Format("bytes={0}-{1}", range.From, range.To - 1);
            HttpWebResponse resp = (HttpWebResponse)(await req.GetResponseAsync());
            //abort the request
            if (req != null)
                req.Abort();
            return resp.GetResponseStream();
        }
    }
}
