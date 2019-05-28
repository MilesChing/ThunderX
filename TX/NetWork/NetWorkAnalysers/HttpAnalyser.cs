using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;
using TX.Strings;
using TX.Models;

namespace TX.NetWork.NetWorkAnalysers
{
    class HttpAnalyser : AbstractAnalyser
    {
        private const string KEY_LEGITIMACY = "Legitimacy";
        private const string KEY_MULTITHREAD = "Multithread";

        private HttpWebResponse _hresp_ = null;

        public override string GetRecommendedName()
        {
            if (_hresp_ == null) return null;
            //https://blog.csdn.net/ash292340644/article/details/52412674
            try
            {
                string fileinfo = (_hresp_).Headers["Content-Disposition"];
                string mathkey = "filename=";
                //当response头中没有Content-Disposition信息时返回从url中截取的文件名
                string name = null;
                if (fileinfo != null) name = fileinfo.Substring(fileinfo.LastIndexOf(mathkey)).Replace(mathkey, "");
                else name = Path.GetFileName(_hresp_.ResponseUri.OriginalString);
                //name是从链接中分析到的全名
                string contentType = _hresp_.ContentType;
                if (contentType.Contains(';')) contentType = contentType.Split(';')[0];
                if (name.Length >= 32) name = name.Substring(name.Length - 32);
                //根据contentType推测扩展名
                if (contentType == null) contentType = "text/html";
                string extent = Converters.ExtentionConverter.TryGetExtention(contentType);
                if (extent == ".*") extent = ".unknown";
                if (name.EndsWith(extent) || extent.Equals("octet-stream")) return name;
                else return name + extent;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override long GetStreamSize()
        {
            return _hresp_ != null ? _hresp_.ContentLength : 0;
        }

        public override AbstractDownloader GetDownloader()
        {
            if (_hresp_ == null) return null;
            var size = GetStreamSize();
            Debug.WriteLine("建立Http下载器：" + (size > 0 ? "HttpDownloader" : "HttpSystemDownloader"));
            //若无法获取size则返回系统下载器处理
            if (size > 0)
                return new HttpDownloader();
            else return new HttpSystemDownloader();
        }

        public override void Dispose()
        {
            Controller?.RemoveMessage(this, KEY_LEGITIMACY);
            Controller?.RemoveMessage(this, KEY_MULTITHREAD);
            Controller?.SetSubmitButtonEnabled(this, false);
            Controller?.SetThreadLayoutVisibility(this, false);
            Controller?.SetRecommendedName(this, AppResources.GetString("Unknown"), 0.5);
            Controller?.RemoveAnalyser(this);

            _hresp_?.Dispose();
            _hresp_ = null;
            URL = null;

            GC.Collect();
        }

        public override bool IsLegal()
        {
            Debug.WriteLine("检测链接 " + URL + " " + _hresp_ == null ? "非法" : "合法");
            return _hresp_ != null;
        }

        private async Task GetResponseAsync()
        {
            try
            {
                if (_hresp_ != null) _hresp_.Dispose();
                _hresp_ = null;
                if (Converters.UrlConverter.MaybeLegal(URL))
                {
                    HttpWebRequest req = WebRequest.CreateHttp(URL);
                    _hresp_ = (HttpWebResponse)await req.GetResponseAsync();
                }
            }
            catch (Exception)
            {
                _hresp_ = null;
                return;
            }
        }

        public override async Task SetURLAsync(string url)
        {
            URL = url;

            Controller?.SetRecommendedName(this, Path.GetFileName(url), 0.5);
            Controller?.UpdateMessage(this, KEY_LEGITIMACY,
                new PlainTextMessage(AppResources.GetString("Connecting")));

            await GetResponseAsync();
            if (_hresp_ == null)
                Controller?.UpdateMessage(this, KEY_LEGITIMACY,
                    new PlainTextMessage(AppResources.GetString("UnableToConnect")));
            else
            {
                Controller?.UpdateMessage(this, KEY_LEGITIMACY,
                    new PlainTextMessage(AppResources.GetString("SuccessfullyConnected")));
                Controller?.SetSubmitButtonEnabled(this, true);

                if (GetStreamSize() > 0)
                {
                    Controller?.SetThreadLayoutVisibility(this, true);
                    Controller?.UpdateMessage(this, KEY_MULTITHREAD,
                        new PlainTextMessage(AppResources.GetString("Multithread")));
                }
                else
                {
                    Controller?.SetThreadLayoutVisibility(this, false);
                    Controller?.RemoveMessage(this, KEY_MULTITHREAD);
                }

                Controller?.SetRecommendedName(this, GetRecommendedName(), 1);
            }
        }
    }
}
