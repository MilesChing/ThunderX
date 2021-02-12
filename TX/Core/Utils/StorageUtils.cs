using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Core.Utils
{
    public static class StorageUtils
    {
        public static async Task MoveContentToAsync(this IStorageFolder now, IStorageFolder destination)
        {
            var files = await now.GetFilesAsync();
            foreach (var file in files)
            {
                await file.MoveAsync(destination, file.Name,
                    NameCollisionOption.GenerateUniqueName);
            }
            var folders = await now.GetFoldersAsync();
            foreach (var folder in folders)
            {
                var nowDes = await destination.CreateFolderAsync(folder.Name, 
                    CreationCollisionOption.GenerateUniqueName);
                await now.MoveContentToAsync(nowDes);
            }

            await now.DeleteAsync();
        }

        public static async Task<IStorageItem> GetStorageItemAsync(string path)
        {
            var rootFolder = await StorageFolder.GetFolderFromPathAsync(
                Path.GetDirectoryName(path));
            return await rootFolder.GetItemAsync(
                Path.GetRelativePath(rootFolder.Path, path));
        }

        public static async Task<long> GetSizeAsync(this IStorageItem now)
        {
            if (now is IStorageFolder folder)
            {
                var items = await folder.GetItemsAsync();
                if (items.Count == 0) return 0L;
                var subTasks = new Task<long>[items.Count];
                for (int i = 0; i < items.Count; ++i)
                    subTasks[i] = items[i].GetSizeAsync();
                long res = 0;
                foreach (var subTask in subTasks)
                {
                    await subTask;
                    res += subTask.Result;
                }
                return res;
            }
            else if (now is IStorageFile file)
            {
                var props = await file.GetBasicPropertiesAsync();
                return (long) props.Size;
            }
            else return 0L;
        }

        public static async Task<IStorageFile> GetOrCreateAnnounceUrlsFileAsync()
        {
            string announceUrlsFileName = "TXAnnounceUrlList.txt";
            var item = await ApplicationData.Current
                .LocalFolder.TryGetItemAsync(announceUrlsFileName);
            if (item != null && item is IStorageFile file) return file;
            else
            {
                if (item != null) await item.DeleteAsync();
                var template = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Resources/ConfigTemplates/AnnounceUrlListTemplate.txt"));
                return await template.CopyAsync(ApplicationData.Current.LocalFolder, announceUrlsFileName);
            }
        }
    }
}
