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
        /*
        /// <summary>
        /// Resort by DownloadBar.downloader.Message.IsDone.
        /// </summary>
        public void ResortDownloadBars()
        {
            lock (downloadBarCollectionLock)
            {
                int completed = collection.Count - 1;
                for (int i = collection.Count - 1; i >= 0; --i)
                {
                    if (collection[i].downloader.Message.IsDone)
                    {
                        if (completed == i)
                        {
                            --completed;
                            continue;
                        }
                        else
                        {
                            collection.Move(i, completed);
                            --completed;
                            ++i;
                        }
                    }
                }
            }
        }
        */
        public void Invoke(Action<ObservableCollection<DownloadBar>> act)
        {
            lock (downloadBarCollectionLock)
            {
                act(collection);
            }
        }
    }
}
