using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace TX.PersistentActions
{
    /// <summary>
    /// ActivationRecord contains information of
    /// the activations of persist actions.
    /// </summary>
    public struct ActivationRecord
    {
        /// <summary>
        /// Key of the action.
        /// </summary>
        public string Key;

        /// <summary>
        /// Version of the application who activate 
        /// this action last time.
        /// </summary>
        public PackageVersion LastActivationVersion;

        /// <summary>
        /// Total count of activations.
        /// </summary>
        public int ActivationCount;

        /// <summary>
        /// Date and time of the last activation.
        /// </summary>
        public DateTime LastActivationTime;
    }
}
