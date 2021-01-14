using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Interfaces;
using Windows.Storage;
using Windows.Storage.AccessCache;
using TX.Utils;

namespace TX.Core.Providers
{
    public class LocalFolderManager : IFolderProvider
    {
        public async Task<IStorageFolder> GetFolderFromTokenAsync(string token)
        {
            try
            {
                if (StorageApplicationPermissions
                    .FutureAccessList.ContainsItem(token))
                    return await StorageApplicationPermissions
                        .FutureAccessList.GetFolderAsync(token);
                else return await GetOrCreateDownloadFolderAsync();
            }
            catch (Exception)
            {
                return await GetOrCreateDownloadFolderAsync();
            }
        }

        public string StoreFolder(IStorageFolder folder) =>
            StorageApplicationPermissions.FutureAccessList.Add(folder);

        public static async Task<IStorageFolder> GetOrCreateDownloadFolderAsync()
        {
            try
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFolderAsync(
                    SettingEntries.DownloadsFolderToken);
            }
            catch (Exception)
            {
                var folder = await DownloadsFolder.CreateFolderAsync(
                    "Thunder X Downloaded", CreationCollisionOption.GenerateUniqueName);
                SettingEntries.DownloadsFolderToken =
                    StorageApplicationPermissions.MostRecentlyUsedList.Add(folder);
                return folder;
            }
        }

        private static Settings SettingEntries = new Settings();
    }
}
