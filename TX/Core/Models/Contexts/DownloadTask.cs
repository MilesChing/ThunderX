using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;

namespace TX.Core.Models.Contexts
{
    public class DownloadTask
    {
        /// <summary>
        /// The identifier of this Task which is unique in a DownloaderManager.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The target of this download task.
        /// </summary>
        public AbstractTarget Target { get; private set; }

        /// <summary>
        /// Supposed name of the destination file,
        /// may different from the final result.
        /// </summary>
        public string DestinationFileName
        {
            get
            {
                if (destinationFileName == null)
                    return Target.SuggestedName;
                else return destinationFileName;
            }

            private set
            {
                destinationFileName = value;
            }
        }
        private string destinationFileName = null;

        /// <summary>
        /// Key of the destination folder
        /// specified by user when a task is created.
        /// This might not be key of the final folder.
        /// </summary>
        public string DestinationFolderKey { get; private set; }

        /// <summary>
        /// Time of the creation of current task.
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Is this task allowed to be downloaded 
        /// automatically by background task.
        /// </summary>
        public bool IsBackgroundDownloadAllowed { get; private set; }

        /// <summary>
        /// Initialize a DownloadTask with a given key
        /// </summary>
        public DownloadTask(
            string key, 
            AbstractTarget target, 
            string destinationFileName,
            string destinationFolderKey,
            DateTime creationTime,
            bool isBackgroundDownloadAllowed
        ) { 
            Key = key;
            Target = target;
            DestinationFileName = destinationFileName;
            DestinationFolderKey = destinationFolderKey;
            CreationTime = creationTime;
            IsBackgroundDownloadAllowed = isBackgroundDownloadAllowed;
        }
    }
}
