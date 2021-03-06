using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace TX.Background
{
    /// <summary>
    /// AbstractBackgroundTask is an abstract of background
    /// tasks managed by BackgroundTaskManager.
    /// </summary>
    abstract class AbstractBackgroundTask
    {
        /// <summary>
        /// Name of the background task.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Run background task with given instance.
        /// </summary>
        /// <param name="taskInstance">Background task instance.</param>
        public abstract void Run(IBackgroundTaskInstance taskInstance);

        /// <summary>
        /// Get a builder which build the background task.
        /// This abstract method is used for successors to append
        /// their triggers and conditions before registered.
        /// </summary>
        /// <returns>Builder which build current background task.</returns>
        public abstract BackgroundTaskBuilder Build();
    }
}
