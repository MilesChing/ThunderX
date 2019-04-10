using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TX.Models;

namespace TX.NetWork
{
    /// <summary>
    /// 执行一些联网操作
    /// </summary>
    class HttpNetWorkMethods
    {
        /// <summary>
        /// 从url获取网络信息
        /// </summary>
        public static async Task<Models.DownloaderMessage> GetMessageAsync(string url)
        {
            Models.DownloaderMessage message = new Models.DownloaderMessage();
            
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp(url);
                HttpWebResponse res = (HttpWebResponse)(await req.GetResponseAsync());

                message.URL = url;

                //文件全名 XXX.XXX
                string fullName = GetResponseName(res);

                message.FileName = System.IO.Path.GetFileNameWithoutExtension(fullName);
                message.Extention = System.IO.Path.GetExtension(fullName);
                message.FileSize = GetResponseSize(res);

                if (res != null)
                {
                    res.Dispose();
                    res = null;
                }

                if (req != null)
                {
                    req.Abort();
                    req = null;
                }

                GC.Collect();
                return message;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
                throw e;
            }
        }

        /// <summary>
        /// 从HTTP应答的头部获取文件全名
        /// </summary>
        public static string GetResponseName(HttpWebResponse response)
        {
            //https://blog.csdn.net/ash292340644/article/details/52412674
            string fileinfo = response.Headers["Content-Disposition"];
            string mathkey = "filename=";
            //当response头中没有Content-Disposition信息时返回从url中截取的文件名
            string name = null;
            if (fileinfo != null) name = fileinfo.Substring(fileinfo.LastIndexOf(mathkey)).Replace(mathkey, "");
            else name = Path.GetFileNameWithoutExtension(response.ResponseUri.OriginalString) +
                    Path.GetExtension(response.ResponseUri.OriginalString);

            string contentType = response.Headers["content-type"];
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
        public static long GetResponseSize(HttpWebResponse response)
        {
            return response.ContentLength;
        }

        /// <summary>
        /// 从某url申请数据流（片段）
        /// </summary>
        /// <param name="url">目标连接</param>
        /// <param name="from">起点偏移</param>
        /// <param name="to">终点偏移</param>
        public static async Task<Stream> GetResponseStreamAsync(string url, long from, long to)
        {
            GC.Collect();
            HttpWebRequest req = WebRequest.CreateHttp(url);
            req.Method = "GET";
            req.Headers["range"] = "bytes=" + from.ToString() + "-" + (to+10).ToString();
            HttpWebResponse resp = (HttpWebResponse)(await req.GetResponseAsync());
            if (req != null)
            {
                req.Abort();
                req = null;
            }

            if (resp == null) return null;
            //返回响应流
            return resp.GetResponseStream();
        }
    }
}
