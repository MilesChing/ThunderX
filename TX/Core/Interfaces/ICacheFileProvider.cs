using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Core.Interfaces
{
    public interface ICacheFileProvider
    {
        Task<string> NewCacheFileAsync();

        IStorageFile GetCacheFileByToken(string token);
    }
}
