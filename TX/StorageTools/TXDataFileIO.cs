using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace TX.StorageTools
{
    /// <summary>
    /// 用于读取和存储mls文件
    /// 提供了对于下载记录的读取和写入文件的方法
    /// </summary>
    public static class TXDataFileIO
    {
        readonly static string fileName = "log.mls";

        public static async Task SaveDownloadMessagesAsync(List<Models.DownloaderMessage> messages)
        {
            try
            {
                string str = JsonHelper.SerializeObject(messages);
                var file = await ApplicationData.Current.LocalCacheFolder
                    .CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, str);
            }
            catch (Exception) { }
        }

        private static bool loaded = false;

        private static List<Models.DownloaderMessage> _list_ = null;

        public static async void StartInitializeMessages()
        {
            try
            {
                var file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync(fileName);
                string str = await FileIO.ReadTextAsync(file);
                await file.DeleteAsync();
                if (str == null) return;
                var list = JsonHelper.DeserializeJsonToList<Models.NullableAttributesDownloaderMessage>(str);
                //首先解析为NullableAttributesDownloaderMessage
                //接下来通过类型转换为DownloaderMessage解决某些属性解析不出来的问题
                _list_ = list.ConvertAll(new Converter<Models.NullableAttributesDownloaderMessage,
                    Models.DownloaderMessage>((old) => { return new Models.DownloaderMessage(old); }));
            }
            catch (Exception e) { Debug.WriteLine(e.Message); }
            finally { loaded = true; }
        }

        public static async Task<List<Models.DownloaderMessage>> GetMessagesAsync()
        {
            while (!loaded) await Task.Delay(20);
            return _list_;
        }
    }
}
