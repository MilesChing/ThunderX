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
        /// Path of the destination file or folder.
        /// </summary>
        public string DestinationPath { get; private set; }

        /// <summary>
        /// Time of the creation of current history.
        /// Also the end time of downloading.
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Initialize a DownloadHistory.
        /// </summary>
        /// <param name="taskKey">Key of the task of the history.</param>
        /// <param name="destinationPath">Path of the destination file or folder.</param>
        /// <param name="creationTime">Time of the creation of current history.</param>
        public DownloadHistory(string taskKey, string destinationPath, DateTime creationTime)
        {
            this.TaskKey = taskKey;
            this.DestinationPath = destinationPath;
            this.CreationTime = creationTime;
        }

        public override bool Equals(object obj)
        {
            if (obj is DownloadHistory history)
                return TaskKey.Equals(history.TaskKey);
            else return base.Equals(obj);
        }

        public override int GetHashCode() => TaskKey.GetHashCode();
    }
}
