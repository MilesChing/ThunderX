using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TX.Downloaders
{
    class Test
    {
        public void Start()
        {
            WebClient client = new WebClient();
            Uri uri = new Uri("https://dldir1.qq.com/qqfile/qq/PCTIM2.3.2/21158/TIM2.3.2.21158.exe");
            string file = Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path + @"/tim.exe";
            client.DownloadFileAsync(uri,file);
            Debug.WriteLine(file);
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            Debug.WriteLine("下载开始");
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Debug.WriteLine("下载中："+e.ProgressPercentage+"% "+e.BytesReceived+"/"+e.TotalBytesToReceive);
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Debug.WriteLine("下载结束");
        }
    }
}
