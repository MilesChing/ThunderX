using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Downloaders;

namespace TX.Controls
{
    interface IDownloaderViewable
    {
        void BindDownloader(AbstractDownloader downloader);

        void ClearDownloaderBinding();
    }
}
