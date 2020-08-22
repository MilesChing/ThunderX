using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;
using TX.Models;
using TX.Strings;
using Windows.Networking.Sockets;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace TX.NetWork.NetWorkAnalysers
{
    class YouTubeAnalyser : AbstractAnalyser
    {
        private const string KEY_YOUTUBE = "YouTubeURL";

        private AbstractAnalyser innerAnalyser = null;

        public override void Dispose()
        {
            innerAnalyser?.Dispose();
            Visual.SetComboBoxLayoutVisibility(false);
            Visual.SetVersionSelector(Array.Empty<ComboBoxData>(), null);
            Visual.RemoveMessage(KEY_YOUTUBE);

            GC.Collect();
        }

        public override AbstractDownloader GetDownloader()
        {
            return innerAnalyser?.GetDownloader();
        }

        public override string GetRecommendedName()
        {
            return innerAnalyser?.GetRecommendedName();
        }

        public override long GetStreamSize()
        {
            return innerAnalyser == null ? 0 : innerAnalyser.GetStreamSize();
        }

        public override bool IsLegal()
        {
            return innerAnalyser == null ? false : innerAnalyser.IsLegal();
        }

        private class YoutubeVideoComboBoxData : ComboBoxData
        {
            public IStreamInfo Info;
            protected override string FormatText()
            {
                if (Info is MuxedStreamInfo)
                {
                    var muxed = (MuxedStreamInfo)Info;
                    return string.Format("V&A: {0} | {1} | {2} , Size: {3}", 
                        muxed.Container.Name, muxed.Resolution, muxed.Framerate, muxed.Size);
                }
                else if (Info is VideoOnlyStreamInfo)
                {
                    var muxed = (VideoOnlyStreamInfo)Info;
                    return string.Format("V Only: {0} | {1} | {2} , Size: {3}",
                        muxed.Container.Name, muxed.Resolution, muxed.Framerate, muxed.Size);
                }
                else if (Info is AudioOnlyStreamInfo)
                {
                    var muxed = (AudioOnlyStreamInfo)Info;
                    return string.Format("A Only: {0} | {1} , Size: {2}",
                        muxed.Container.Name, Info.Bitrate, muxed.Size);
                }

                return "Unknown Stream Info";
            }
        }

        public override async Task SetURLAsync(string url)
        {
            try
            {
                Visual.UpdateMessage(KEY_YOUTUBE, new PlainTextMessage(
                    AppResources.GetString("YouTubeLinkDetectedButWaiting")));
                var client = new YoutubeClient();
                var video = await client.Videos.GetAsync(url);
                var manifests = await client.Videos.Streams.GetManifestAsync(video.Id);
                if (manifests.Streams.Count <= 0) throw new Exception();

                Visual.UpdateMessage(KEY_YOUTUBE,
                    new PlainTextMessage(
                        string.Format("{0}\nTitle: {1} Author: {2}",
                            AppResources.GetString("YouTubeLinkDetected"),
                            video.Title, video.Author)
                    )
                );

                List<ComboBoxData> versions = new List<ComboBoxData>();
                foreach (var info in manifests.Streams)
                    versions.Add(new YoutubeVideoComboBoxData() {
                        Info = info
                    });

                Visual.SetVersionSelector(versions.ToArray(), (item) =>
                {
                    _ = Task.Run(() =>
                        {
                            Visual.SetComboBoxLayoutVisibility(false);
                            try
                            {
                                innerAnalyser?.Dispose();
                                var info = item as YoutubeVideoComboBoxData;
                                if (info == null) return;
                                string target = info.Info.Url;
                                if (target != null)
                                {
                                    innerAnalyser = Converters.UrlConverter.GetAnalyser(target);
                                    if (innerAnalyser != null)
                                    {
                                        URL = target;
                                        innerAnalyser.BindVisualController(Visual);
                                        innerAnalyser.SetURLAsync(target).Wait();
                                    }
                                }
                            }
                            finally
                            {
                                Visual.SetComboBoxLayoutVisibility(true);
                            }
                        }
                    );
                });

                Visual.SetComboBoxLayoutVisibility(true);
            }
            catch (Exception)
            {
                Visual.UpdateMessage(KEY_YOUTUBE, new PlainTextMessage(
                    AppResources.GetString("YouTubeLinkDetectedButFailed")));
                Visual.SetComboBoxLayoutVisibility(false);
            }
        }
    }
}
