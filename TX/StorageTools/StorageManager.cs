using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Controls;
using Windows.Storage;

namespace TX.StorageTools
{
    class StorageManager
    {
        public static string GetTemporaryName()
        {
            Random rd = new Random();
            const string source = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            //固定文件名长度
            const int fileNameLength = 20;
            string fileName = "";
            //生成随机名称
            for (int i = 1; i <= fileNameLength; i++)
                fileName += source[rd.Next() % source.Length];
            return fileName;
        }

        /// <summary>
        /// 获取临时文件
        /// </summary>
        public static async Task<string> GetTemporaryFileAsync()
        {
            StorageFolder folder = ApplicationData.Current.LocalCacheFolder;
            return (await folder.CreateFileAsync(GetTemporaryName(), CreationCollisionOption.GenerateUniqueName)).Path;
        }

        public static async Task SaveDownloadMessagesAsync(List<Models.DownloaderMessage> messages)
        {
            try
            {
                string str = JsonHelper.SerializeObject(messages);
                var file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("log.mls", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, str);
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public static async Task<List<Models.DownloaderMessage>> GetMessagesAsync()
        {
            StorageFile file = null;
            try { file = await ApplicationData.Current.LocalCacheFolder.GetFileAsync("log.mls"); }
            catch (Exception) { return null; }
            try
            {
                string str = await FileIO.ReadTextAsync(file);
                await file.DeleteAsync();
                if (str == null) return null;
                return JsonHelper.DeserializeJsonToList<Models.DownloaderMessage>(str);
            }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// 删除缓存文件夹中没用的东西
        /// </summary>
        public static async Task GetCleanAsync()
        {
            try
            {
                ulong size = 0;
                StorageFolder folder = ApplicationData.Current.LocalCacheFolder;
                var files = await folder.GetFilesAsync();
                foreach (StorageFile file in files)
                {
                    if (file.Name.Equals("log.mls")) continue;
                    bool remove = true;
                    foreach (DownloadBar bar in MainPage.Current.DownloadBarCollection)
                        if (bar.downloader.Message.TempFilePath.Equals(file.Path))
                        {
                            remove = false;
                            break;
                        }
                    if (remove)
                    {
                        size += (await file.GetBasicPropertiesAsync()).Size;
                        await file.DeleteAsync();
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
