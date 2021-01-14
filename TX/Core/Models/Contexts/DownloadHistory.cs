using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Contexts
{
    public class DownloadHistory
    {
        /// <summary>
        /// Key of the task of the history specified by user when created.
        /// </summary>
        public string TaskKey { get; private set; }

        /// <summary>
        /// Path of the destination file.
        /// </summary>
        public string DestinationFilePath { get; private set; }

        /// <summary>
        /// Time of the creation of current history.
        /// Also the end time of downloading.
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Initialize a DownloadHistory.
        /// </summary>
        /// <param name="taskKey">Key of the task of the history.</param>
        /// <param name="destinationFilePath">Path of the destination file.</param>
        /// <param name="creationTime">Time of the creation of current history.</param>
        public DownloadHistory(string taskKey, string destinationFilePath, DateTime creationTime)
        {
            this.TaskKey = taskKey;
            this.DestinationFilePath = destinationFilePath;
            this.CreationTime = creationTime;
        }
    }
}
