using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Core.Interfaces
{
    public interface ICacheStorageProvider
    {
        Task<string> NewCacheFileAsync();

        Task<string> NewCacheFolderAsync();

        Task<IStorageFile> GetCacheFileByTokenAsync(string token);

        Task<IStorageFolder> GetCacheFolderByTokenAsync(string token);
    }
}
