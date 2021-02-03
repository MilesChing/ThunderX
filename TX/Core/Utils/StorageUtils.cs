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
    }
}
