using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace TX.Core.Interfaces
{
    public interface IFolderProvider
    {
        Task<IStorageFolder> GetFolderFromTokenAsync(string token); 
    }
}
