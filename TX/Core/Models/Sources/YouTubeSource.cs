using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace TX.Core.Models.Sources
{
    class YouTubeSource : AbstractSource, IMultiTargetsExtracted
    {
        public YouTubeSource(Uri uri) : base(uri) { }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetTargetInfosAsync()
        {
            var video = await client.Videos.GetAsync(Uri.OriginalString);
            var manifests = await client.Videos.Streams.GetManifestAsync(video.Id);
            Ensure.That(manifests.Streams.Count, nameof(manifests.Streams.Count)).IsGt(0);
            var resList = new List<KeyValuePair<string, string>>();
            foreach (var info in manifests.Streams)
                resList.Add(new KeyValuePair<string, string>(
                    info.Url, FormatText(info)));
            return resList;
        }

        public async Task<AbstractTarget> GetTargetAsync(IEnumerable<string> keys)
        {
            Ensure.That(keys.Count()).Is(1);
            var source = CreateSource(new Uri(keys.First())) as ISingleTargetExtracted;
            Ensure.That(source).IsNotNull();
            return await source.GetTargetAsync();
        }

        public bool IsMultiSelectionSupported => false;

        public static bool IsValid(Uri uri) => uri.Host.Equals("www.youtube.com") && uri.LocalPath.Equals("/watch");

        private string FormatText(IStreamInfo info)
        {
            if (info is MuxedStreamInfo muxed)
                return string.Format("V&A: {0} | {1} | {2} , Size: {3}",
                    muxed.Container.Name, muxed.Resolution, muxed.Framerate, muxed.Size);
            else if (info is VideoOnlyStreamInfo vo)
                return string.Format("V Only: {0} | {1} | {2} , Size: {3}",
                    vo.Container.Name, vo.Resolution, vo.Framerate, vo.Size);
            else if (info is AudioOnlyStreamInfo ao)
                return string.Format("A Only: {0} | {1} , Size: {2}",
                    ao.Container.Name, ao.Bitrate, ao.Size);

            return "Unknown Stream Info";
        }

        private static readonly YoutubeClient client = new YoutubeClient();
    }
}
