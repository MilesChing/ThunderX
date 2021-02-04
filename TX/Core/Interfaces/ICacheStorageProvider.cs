using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Core.Interfaces
{
    public interface ICacheStorageProvider
    {
        Task<string> NewCacheFileAsync();

        Task<string> NewCacheFolderAsync();

        IStorageFile GetCacheFileByToken(string token);

        IStorageFolder GetCacheFolderByToken(string token);
    }
}
