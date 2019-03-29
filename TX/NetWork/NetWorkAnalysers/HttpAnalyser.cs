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
        private async Task<HttpWebResponse> GetReponseAsync()
        {
            if (_hresp_ == null)
            {
                HttpWebRequest req = WebRequest.CreateHttp(url);
                return _hresp_ = (HttpWebResponse) await req.GetResponseAsync();
            }
            else return _hresp_;
        }

        public HttpAnalyser(string url)
        {
            this.url = url;
            Debug.WriteLine("建立Http分析器：Url=" + url);
        }
        
        /// <summary>
        /// 从HTTP应答的头部获取文件全名
        /// </summary>
        public async Task<string> GetRecommendedNameAsync()
        {
            //https://blog.csdn.net/ash292340644/article/details/52412674
            string fileinfo = (await GetReponseAsync()).Headers["Content-Disposition"];
            string mathkey = "filename=";
            //当response头中没有Content-Disposition信息时返回从url中截取的文件名
            string name = null;
            if (fileinfo != null) name = fileinfo.Substring(fileinfo.LastIndexOf(mathkey)).Replace(mathkey, "");
            else name = Path.GetFileNameWithoutExtension((await GetReponseAsync()).ResponseUri.OriginalString) +
                    Path.GetExtension((await GetReponseAsync()).ResponseUri.OriginalString);

            string contentType = (await GetReponseAsync()).Headers["content-type"];
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

        /// <summary>
        /// 从HTTP应答的头部获取文件大小
        /// </summary>
        public async Task<long> GetResponseSizeAsync()
        {
            return (await GetReponseAsync()).ContentLength;
        }
        
        /// <summary>
        /// 若无法部署线程（未使用ContentLength）按照系统下载处理
        /// </summary>
        public async Task<IDownloader> GetDownloaderAsync()
        {
            var size = await GetResponseSizeAsync();
            Debug.WriteLine("建立Http下载器："+(size>0?"HttpDownloader":"HttpSystemDownloader"));
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

        /// <summary>
        /// 成功收到则合法
        /// </summary>
        public async Task<bool> CheckUrlAsync()
        {
            try
            {
                await GetResponseSizeAsync();
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
    }
}
