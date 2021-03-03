using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;
using MonoTorrent;
using MonoTorrent.Client;
using System.Threading;

namespace TX.Core.Models.Sources
{
    class MagnetSource : AbstractSource, ISingleSubsourceExtracted
    {
        public MagnetSource(Uri uri, ClientEngine engine, IEnumerable<string> announceUrls) : base(uri)
        {
            Ensure.That(uri.Scheme).Is("magnet");
            Ensure.That(engine).IsNotNull();
            Ensure.That(announceUrls).IsNotNull();
            this.engine = engine;
            this.announceUrls = announceUrls;
            link = MagnetLink.FromUri(Uri);
        }

        public async Task<AbstractSource> GetSubsourceAsync()
        {
            if (!link.AnnounceUrls.IsReadOnly)
                foreach (var url in announceUrls)
                    link.AnnounceUrls.Add(url);
            CancellationTokenSource ctk = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var metaData = await engine.DownloadMetadataAsync(link, ctk.Token);
            return new TorrentSource(Uri, metaData);
        }

        private readonly MagnetLink link = null;
        private readonly ClientEngine engine = null;
        private readonly IEnumerable<string> announceUrls = null;
    }
}
