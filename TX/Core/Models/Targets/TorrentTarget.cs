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

            DisplayedUri = displayedUri;
            Torrent = Torrent.Load(torrentBytes);

            this.torrentBytes = torrentBytes;
            this.selectedFiles = selectedFiles;
            foreach (var file in Torrent.Files)
            {
                if (selectedFiles.Contains(file.Path))
                    file.Priority = Priority.Normal;
                else file.Priority = Priority.DoNotDownload;
            }
        }

        [JsonIgnore]
        public Torrent Torrent { get; private set; }

        public Uri DisplayedUri { get; private set; }

        protected override string GetSuggestedName() => Torrent.Name;

        [JsonProperty]
        private readonly string[] selectedFiles = null;
        [JsonProperty]
        private readonly byte[] torrentBytes = null;
    }
}
