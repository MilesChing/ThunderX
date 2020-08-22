using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;
using TX.Models;
using TX.Strings;

namespace TX.NetWork.NetWorkAnalysers
{
    class ThunderAnalyser : AbstractAnalyser
    {
        private AbstractAnalyser innerAnalyser = null;

        private const string KEY_THUNDER = "Thunder";

        public override void Dispose()
        {
            innerAnalyser?.Dispose();
            Visual.RemoveMessage(KEY_THUNDER);
            innerAnalyser = null;

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

        public override async Task SetURLAsync(string url)
        {
            URL = Converters.UrlConverter.TranslateURLThunder(url);
            Visual.UpdateMessage(KEY_THUNDER, new PlainTextMessage(
                AppResources.GetString("ThunderLinkDetected")));
            innerAnalyser?.Dispose();
            innerAnalyser = null;
            innerAnalyser = Converters.UrlConverter.GetAnalyser(URL);
            if (innerAnalyser == null)
            {
                Visual.UpdateMessage(KEY_THUNDER, new PlainTextMessage(
                    AppResources.GetString("ThunderLinkDetectedButFailed")));
                return;
            }

            innerAnalyser.BindVisualController(Visual);
            await innerAnalyser.SetURLAsync(URL);
        }
    }
}
