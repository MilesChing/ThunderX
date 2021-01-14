using EnsureThat;
using Microsoft.Toolkit.Extensions;
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

        public async Task<IEnumerable<KeyValuePair<string, Task<AbstractTarget>>>> GetTargetsAsync()
        {
            var video = await client.Videos.GetAsync(Uri.OriginalString);
            var manifests = await client.Videos.Streams.GetManifestAsync(video.Id);
            Ensure.That(manifests.Streams.Count, nameof(manifests.Streams.Count)).IsGt(0);
            var resList = new List<KeyValuePair<string, Task<AbstractTarget>>>();
            foreach (var infoItr in manifests.Streams)
            {
                var info = infoItr;
                string displayedName = FormatText(info);
                var source = AbstractSource.ConstructSource(new Uri(info.Url));
                if (source is HttpSource httpSource)
                {
                    resList.Add(
                        new KeyValuePair<string, Task<AbstractTarget>>(
                            displayedName,
                            new Task<AbstractTarget>(() =>
                            {
                                var task = httpSource.GetTargetAsync();
                                task.Wait();
                                return task.Result;
                            })
                        )
                    );
                }
            }
            return resList;
        }

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
