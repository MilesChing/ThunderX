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
        public TorrentTarget(byte[] torrentBytes, Uri displayedUri, string[] selectedFiles)
        {
            Ensure.That(torrentBytes, nameof(torrentBytes)).IsNotNull();
            Ensure.That(selectedFiles, nameof(selectedFiles)).IsNotNull();
            Ensure.That(displayedUri, nameof(displayedUri)).IsNotNull();

            this.torrentBytes = torrentBytes;
            this.selectedFiles = selectedFiles;
            DisplayedUri = displayedUri;

            Torrent = Torrent.Load(torrentBytes);
        }

        [JsonIgnore]
        public Torrent Torrent { get; private set; }

        public Uri DisplayedUri { get; private set; }

        public bool IsFileSelected(ITorrentFile file) =>
            selectedFiles.Contains(file.Path);

        protected override string GetSuggestedName() => Torrent.Name;

        [JsonProperty]
        private readonly string[] selectedFiles = null;
        [JsonProperty]
        private readonly byte[] torrentBytes = null;
    }
}
