using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;
using TX.Models;
using TX.Strings;
using YoutubeExplode;

namespace TX.NetWork.NetWorkAnalysers
{
    class YouTubeAnalyser : AbstractAnalyser
    {
        private const string KEY_YOUTUBE = "YouTubeURL";

        private string id = null;

        private YoutubeExplode.Models.Video video = null;

        private YoutubeExplode.Models.MediaStreams.MediaStreamInfoSet infos = null;

        private AbstractAnalyser innerAnalyser = null;

        public override void Dispose()
        {
            innerAnalyser?.Dispose();
            Controller?.SetComboBoxLayoutVisibility(this, false);
            Controller?.SetComboBoxSelectionChangedListener(this, null);
            Controller?.RemoveMessage(this, KEY_YOUTUBE);
            Controller?.ClearComboBoxItem(this);
            Controller?.RemoveAnalyser(this);

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

        private string GetURLFromInfos(string info)
        {
            foreach (var inf in infos.Muxed)
            {
                string target = inf.VideoQuality.ToString() + " - " + inf.VideoEncoding.ToString();
                if (target.Equals(info)) return inf.Url;
            }
            return null;
        }

        public override async Task SetURLAsync(string url)
        {
            try
            {
                id = YoutubeClient.ParseVideoId(url);
                var client = new YoutubeClient();
                Controller?.UpdateMessage(this, KEY_YOUTUBE,
                    new PlainTextMessage(AppResources.GetString("YouTubeLinkDetectedButWaiting")));
                video = await client.GetVideoAsync(id);
                infos = await client.GetVideoMediaStreamInfosAsync(id);
                if (infos.Muxed.Count > 0)
                    Controller?.SetComboBoxLayoutVisibility(this, true);
                else throw new Exception();

                Controller?.UpdateMessage(this, KEY_YOUTUBE,
                    new PlainTextMessage(
                    AppResources.GetString("YouTubeLinkDetectedButWaiting")
                    + " - " +
                    video.Title
                    ));

                foreach (var info in infos.Muxed)
                {
                    PlainTextComboBoxData data = new PlainTextComboBoxData();
                    data.Text = info.VideoQuality.ToString() + " - " + info.VideoEncoding.ToString();
                    Controller?.AddComboBoxItem(this, data);
                }

                Controller?.SetComboBoxSelectionChangedListener(this, 
                    async (item) => {
                        innerAnalyser?.Dispose();
                        string target = GetURLFromInfos(item.Text);
                        if(target!=null)
                        {
                            innerAnalyser = Converters.UrlConverter.GetAnalyser(target);
                            if (innerAnalyser != null)
                            {
                                URL = target;
                                innerAnalyser.BindVisualController(Controller);
                                Controller?.RegistAnalyser(this, innerAnalyser);
                                await innerAnalyser.SetURLAsync(target);
                            }
                        }
                    }
                );
            }
            catch (Exception)
            {
                Controller?.UpdateMessage(this, KEY_YOUTUBE,
                    new PlainTextMessage(
                    AppResources.GetString("YouTubeLinkDetectedButFailed")
                    ));
                Controller?.SetComboBoxLayoutVisibility(this, false);
            }
        }
    }
}
