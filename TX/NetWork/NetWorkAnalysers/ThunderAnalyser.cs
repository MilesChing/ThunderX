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
            Controller?.RemoveMessage(this, KEY_THUNDER);
            innerAnalyser = null;
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

        public override async Task SetURLAsync(string url)
        {
            URL = Converters.UrlConverter.TranslateURLThunder(url);
            Controller?.UpdateMessage(this, KEY_THUNDER, new PlainTextMessage(
                AppResources.GetString("ThunderLinkDetected")));
            innerAnalyser?.Dispose();
            innerAnalyser = null;
            innerAnalyser = Converters.UrlConverter.GetAnalyser(URL);
            if (innerAnalyser == null)
            {
                Controller?.UpdateMessage(this, KEY_THUNDER, new PlainTextMessage(
                    AppResources.GetString("ThunderLinkDetectedButFailed")));
                return;
            }

            Controller?.RegistAnalyser(this, innerAnalyser);
            innerAnalyser.BindVisualController(Controller);
            await innerAnalyser.SetURLAsync(URL);
        }
    }
}
