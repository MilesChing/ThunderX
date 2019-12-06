using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Controls
{
    public class DownloadBarManager
    {
        public DownloadBarManager(ObservableCollection<DownloadBar> collection, object collection_lock)
        {
            this.collection = collection;
            this.downloadBarCollectionLock = collection_lock;
        }

        private ObservableCollection<DownloadBar> collection;

        private object downloadBarCollectionLock;
        
        public void Invoke(Action<ObservableCollection<DownloadBar>> act)
        {
            lock (downloadBarCollectionLock)
            {
                act(collection);
            }
        }
    }
}
