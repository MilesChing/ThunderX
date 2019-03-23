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
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<Models.DownloaderMessage> GetMessageAsync(string url)
        {
            Models.DownloaderMessage message = new Models.DownloaderMessage();

            //获取response
            HttpWebResponse resp = await GetResponseAsync(url);

            message.URL = url;

            //文件全名 XXX.XXX
            string fullName = GetResponseName(resp);
            fullName = (fullName == null ? url : fullName); //从应答找不到文件名时自动从url截取
            message.FileName = System.IO.Path.GetFileNameWithoutExtension(fullName);
            message.TypeName = System.IO.Path.GetExtension(fullName);
            message.FileSize = GetResponseSize(resp);

            if (resp != null){
                resp.Dispose();
                resp = null;
            }

            return message;
        }

        /// <summary>
        /// 从HTTP应答的头部获取文件全名
        /// </summary>
        /// <returns></returns>
        public static string GetResponseName(HttpWebResponse response)
        {
            //https://blog.csdn.net/ash292340644/article/details/52412674
            string fileinfo = response.Headers["Content-Disposition"];
            string mathkey = "filename=";
            //当response头中没有Content-Disposition信息时返回从url中截取的文件名
            if (fileinfo == null) return null;
            return fileinfo.Substring(fileinfo.LastIndexOf(mathkey)).Replace(mathkey, "");
        }

        /// <summary>
        /// 从HTTP应答的头部获取文件大小
        /// </summary>
        /// <param name="response"></param>
        /// <returns>文件大小</returns>
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
        /// <returns></returns>
        public static async Task<Stream> GetResponseStreamAsync(string url, long from, long to)
        {
            System.GC.Collect();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
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

        public static async Task<HttpWebResponse> GetResponseAsync(string url)
        {
            System.GC.Collect();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";

            Debug.WriteLine(url);

            HttpWebResponse resp;
            try{ resp = (HttpWebResponse)(await req.GetResponseAsync()); }
            catch(Exception e) {
                Debug.WriteLine(e.ToString());
                return null;
            }
            
            if (req != null)
            {
                req.Abort();
                req = null;
            }

            switch (resp.StatusCode)
            {
                case HttpStatusCode.OK:
                    return resp;
                case HttpStatusCode.Redirect:
                case HttpStatusCode.Moved:
                case HttpStatusCode.RedirectKeepVerb:
                case HttpStatusCode.Ambiguous:
                    if (resp.Headers.AllKeys.Contains("Location"))
                    {
                        string newUrl = resp.Headers["Location"];
                        resp.Close();
                        return await GetResponseAsync(url);
                    }
                    else return null;
                default:
                    return null;
            }
        }
    }
}
