using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Downloaders;

namespace TX.NetWork.NetWorkAnalysers
{
    class HttpAnalyser : IAnalyser
    {
        private string url = null;
        private HttpWebResponse _hresp_ = null;

        public HttpAnalyser(string url)
        {
            this.url = url;
            Debug.WriteLine("建立Http分析器：Url=" + url);
        }
        
        public string GetRecommendedName()
        {
            //https://blog.csdn.net/ash292340644/article/details/52412674
            string fileinfo = (_hresp_).Headers["Content-Disposition"];
            string mathkey = "filename=";
            //当response头中没有Content-Disposition信息时返回从url中截取的文件名
            string name = null;
            if (fileinfo != null) name = fileinfo.Substring(fileinfo.LastIndexOf(mathkey)).Replace(mathkey, "");
            else name = Path.GetFileNameWithoutExtension(_hresp_.ResponseUri.OriginalString) +
                    Path.GetExtension(_hresp_.ResponseUri.OriginalString);

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
        
        public long GetStreamSize()
        {
            return _hresp_.ContentLength;
        }
        
        public IDownloader GetDownloader()
        {
            var size = GetStreamSize();
            Debug.WriteLine("建立Http下载器："+(size>0?"HttpDownloader":"HttpSystemDownloader"));
            //若无法获取size则返回系统下载器处理
            if (size > 0)
                return new HttpDownloader();
            else return new HttpSystemDownloader();
        }

        public void Dispose()
        {
            Debug.WriteLine("分析器已释放");
            if (_hresp_ != null)
            {
                _hresp_.Dispose();
                _hresp_ = null;
            }
        }
        
        public bool CheckUrl()
        {
            try
            {
                Debug.WriteLine("Http链接合法：Url=" + url);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetUrl()
        {
            return url;
        }

        public async Task GetResponseAsync()
        {
            HttpWebRequest req = WebRequest.CreateHttp(url);
            _hresp_ = (HttpWebResponse)await req.GetResponseAsync();
        }
        
    }
}
