using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using Windows.Storage;

namespace TX.Core.Providers
{
    class TorrentProvider : IPersistable, IDisposable
    {
        public TorrentProvider(EngineSettings defaultSettings = null)
        {
            this.defaultSettings = defaultSettings;
        }

        public async Task InitializeTorrentProviderAsync(byte[] checkpoint = null)
        {
            if (checkpoint != null)
                engine = await ClientEngine.RestoreStateAsync(checkpoint);
            else
                engine = new ClientEngine(defaultSettings);
        }

        public byte[] ToPersistentByteArray() => Task.Run(
            async () => await engine.SaveStateAsync()).Result;

        public async Task CleanEngineTorrentsAsync(IEnumerable<Torrent> activeTorrents)
        {
            var inactiveTorrents = engine.Torrents.Where(
                m => activeTorrents.All(t => !m.Torrent.Equals(t))
            ).ToList();
            foreach (var it in inactiveTorrents)
                await engine.RemoveAsync(it);
        }

        public void Dispose()
        {
            engine?.Dispose();
            engine = null;
        }

        public ClientEngine Engine => engine;

        private readonly EngineSettings defaultSettings = null;
        private ClientEngine engine = null;
    }
}
