using EnsureThat;
using MonoTorrent;
using MonoTorrent.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Utils;
using Windows.Storage;

namespace TX.Core.Models.Targets
{
    public class TorrentTarget : AbstractTarget
    {
        public TorrentTarget(byte[] torrentBytes, Uri displayedUri, string[] selectedFilePaths)
        {
            Ensure.That(torrentBytes, nameof(torrentBytes)).IsNotNull();
            Ensure.That(selectedFilePaths, nameof(selectedFilePaths)).IsNotNull();
            Ensure.That(displayedUri, nameof(displayedUri)).IsNotNull();

            DisplayedUri = displayedUri;
            Torrent = Torrent.Load(torrentBytes);

            this.torrentBytes = torrentBytes;
            this.selectedFilePaths = selectedFilePaths;
            foreach (var file in Torrent.Files)
                if (selectedFilePaths.Contains(file.Path))
                    file.Priority = Priority.Normal;
                else file.Priority = Priority.DoNotDownload;
        }

        [JsonIgnore]
        public Torrent Torrent { get; private set; }

        public Uri DisplayedUri { get; private set; }

        protected override string GetSuggestedName() => Torrent.Name;

        [JsonProperty]
        private readonly string[] selectedFilePaths = null;
        [JsonProperty]
        private readonly byte[] torrentBytes = null;
    }
}
