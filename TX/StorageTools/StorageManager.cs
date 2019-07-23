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
        /// <returns>获取不到返回null</returns>
        public static async Task<StorageFolder> TryGetDownloadFolderAsync()
        {
            try
            {
                return await StorageFolder.GetFolderFromPathAsync(Settings.DownloadFolderPath);
            }
            catch(Exception e)
            {
                Debug.WriteLine("获取DownloadFolder错误：" + e.Message);
                Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("DownloadFolderPathIllegal"),
                    Strings.AppResources.GetString("DownloadFolderPathIllegalMessage"));
                return null;
            }
        }

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
                Debug.WriteLine("写入文件：\n" + str);
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
                var list = JsonHelper.DeserializeJsonToList<Models.NullableAttributesDownloaderMessage>(str);
                //首先解析为NullableAttributesDownloaderMessage
                //接下来通过类型转换为DownloaderMessage解决某些属性解析不出来的问题
                return list.ConvertAll(new Converter<Models.NullableAttributesDownloaderMessage,
                    Models.DownloaderMessage>((old) => { return new Models.DownloaderMessage(old); }));
            }
            catch (Exception) { return null; }
        }

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
