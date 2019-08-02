using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Controls;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace TX.StorageTools
{
    class StorageManager
    {
        public static int FutureAccessListCount()
        {
            var entries = StorageApplicationPermissions.FutureAccessList.Entries;
            return StorageApplicationPermissions.FutureAccessList.Entries.Count;
        }

        public static string AddToFutureAccessList(StorageFolder folder)
        {
            return StorageApplicationPermissions.FutureAccessList.Add(folder);
        }

        public static void RemoveFromFutureAccessList(string token)
        {
            try
            {
                StorageApplicationPermissions.FutureAccessList.Remove(token);
            }
            catch (Exception) { }
        }

        public static void RemoveFromFutureAccessListExcept(string[] tokens)
        {
            var entries = StorageApplicationPermissions.FutureAccessList.Entries;
            string[] tokensToRemove = new string[entries.Count];
            int tail = 0;
            foreach (var p in entries)
                if (!tokens.Contains(p.Token))
                    tokensToRemove[tail++] = p.Token;
            for (int i = 0; i < tail; ++i) StorageManager.RemoveFromFutureAccessList(tokensToRemove[i]);
        }

        public static async Task<StorageFolder> TryGetFolderAsync(string token)
        {
            try
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static void ClearFutureAccessList()
        {
            StorageApplicationPermissions.FutureAccessList.Clear();
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

        public static async void LaunchFileAsync(StorageFile file)
        {
            if (file.FileType.ToLower() == ".exe")
            {
                LaunchFolderAsync(await file.GetParentAsync());
                Toasts.ToastManager.ShowSimpleToast(Strings.AppResources.GetString("ExtentionNotSupported"), "");
                return;
            }
            //如果file不是可执行文件那么用默认软件打开file，否则打开file所在文件夹
            var options = new Windows.System.LauncherOptions();
            options.DisplayApplicationPicker = true;
            await Windows.System.Launcher.LaunchFileAsync(file, options);
        }

        public static async void LaunchFolderAsync(StorageFolder folder)
        {
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
    }
}
