using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;
using TX.NetWork.URLAnalysers;

namespace TX.NetWork.NetWorkAnalysers
{
    class HttpAnalyser : AbstractAnalyser
    {
        private AbstractURLAnalyser[] analysers = { new ThunderURLAnalyser() };

        private string legitimacyInformation = null;

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

                string contentType = _hresp_.Headers["content-type"];
                if (contentType.Contains(';')) contentType = contentType.Split(';')[0];

                if (contentType == null) return name;
                else
                {
                    string extent = Converters.ExtentionConverter.TryGetExtention(contentType);
                    if (extent != ".*" && Path.GetExtension(name) != extent)
                        return name + extent;
                    else return name;
                }
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
            Debug.WriteLine("分析器已释放");
            _hresp_?.Dispose();
            _hresp_ = null;
            URL = null;
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
                    legitimacyInformation = "成功连接到目标服务器";
                }
                else legitimacyInformation = null;
            }
            catch (Exception)
            {
                _hresp_ = null;
                legitimacyInformation = "无法连接：请检查设备的网络连接";
                return;
            }
        }

        public override NewTaskPageVisualDetail GetVisualDetail()
        {
            bool needThreadNum = (GetStreamSize() > 0);
            NewTaskPageVisualDetail detail = new NewTaskPageVisualDetail(needThreadNum);
            List<Models.LinkAnalysisMessage> messages = new List<Models.LinkAnalysisMessage>();

            foreach (AbstractURLAnalyser analyser in analysers)
                if (analyser.Message != null)
                    messages.Add(new Models.LinkAnalysisMessage(analyser.Message));

            if (legitimacyInformation != null)
                messages.Add(new Models.LinkAnalysisMessage(legitimacyInformation));
            
            return new NewTaskPageVisualDetail(needThreadNum, messages.ToArray());
        }

        public override async Task SetURLAsync(string url)
        {
            Dispose();
            URL = url;
            foreach(AbstractURLAnalyser analyser in analysers)
            {
                analyser.OriginalURL = URL;
                URL = analyser.TransferedURL;
            }
            legitimacyInformation = null;
            await GetResponseAsync();
        }
    }
}
