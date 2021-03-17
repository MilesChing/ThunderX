using EnsureThat;
using MonoTorrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;
using TX.Core.Utils;
using Windows.Storage;

namespace TX.Core.Models.Sources
{
    class TorrentSource : AbstractSource, IMultiTargetsExtracted
    {
        public TorrentSource(Uri uri) : base(uri) 
        {
            Ensure.That(uri.IsFile).IsTrue();
            Ensure.That(Path.GetExtension(uri.LocalPath)).Is(".torrent");
        }

        public TorrentSource(Uri uri, byte[] torrentBytes) : base(uri)
        {
            Ensure.That(torrentBytes).IsNotNull();
            this.torrentBytes = torrentBytes;
        }

        public bool IsMultiSelectionSupported => true;

        public Task<AbstractTarget> GetTargetAsync(IEnumerable<string> keys)
        {
            return Task.Run<AbstractTarget>(() =>
            {
                Ensure.That(torrentBytes).IsNotNull();
                return new TorrentTarget(torrentBytes, Uri, keys.ToArray());
            });
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetTargetInfosAsync()
        {      
            if (torrentBytes == null)
            {
                // for files, only Uri.LocalPath is correct to use
                var file = await StorageFile.GetFileFromPathAsync(Uri.LocalPath);
                torrentBytes = (await FileIO.ReadBufferAsync(file)).ToArray();
            }

            torrent = await Torrent.LoadAsync(torrentBytes);
            var list = new List<KeyValuePair<string, string>>();
            foreach (var f in torrent.Files)
                list.Add(new KeyValuePair<string, string>(f.Path, FormatText(f)));
            return list;
        }

        public static bool IsValid(Uri uri) => uri.IsFile && Path.GetExtension(uri.LocalPath).Equals(".torrent");

        private string FormatText(TorrentFile file) => $"{file.Path} - {file.Length.SizedString()}";

        private byte[] torrentBytes = null;
        private Torrent torrent = null;
    }
}
