using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace TX.PersistentActions
{
    /// <summary>
    /// PersistentActionManager records the information of
    /// the previouly activation of some actions.
    /// </summary>
    public class PersistentActionManager
    {
        private readonly Dictionary<string, ActivationRecord> records = 
            new Dictionary<string, ActivationRecord>();

        /// <summary>
        /// Construct a PersistentActionManager with given capacity.
        /// </summary>
        /// <param name="capacity">
        /// Capacity is the maximum count of records permitted
        /// (maximum count of actions with unique keys), 
        /// Records will be dropped (older first) if the actual 
        /// count exceeded the capacity of the manager.
        /// </param>
        public PersistentActionManager(int capacity)
        {
            LoadRecords(capacity);
        }

        /// <summary>
        /// Activate the action and modify its record.
        /// </summary>
        /// <param name="key">Key of this action.</param>
        /// <param name="action">The action.</param>
        public void Activate(string key, Action action)
        {
            if (!records.TryGetValue(key, out ActivationRecord rec))
                rec = new ActivationRecord() 
                { 
                    Key = key, 
                    LastActivationVersion = Package.Current.Id.Version,
                    ActivationCount = 0,
                };
            rec.ActivationCount += 1;
            rec.LastActivationTime = DateTime.Now;
            records[key] = rec;
            action();
        }

        /// <summary>
        /// Try to get the record with specific key.
        /// </summary>
        /// <param name="key">Key of the target record.</param>
        /// <param name="record">The output record.</param>
        /// <returns>
        /// True is returned if there is a record stored.
        /// False means the record with given key is not found.
        /// Action with that key might be not ever activated, 
        /// or might be dropped because of the capacity limit.
        /// </returns>
        public bool TryGetRecord(string key, out ActivationRecord record) =>
            records.TryGetValue(key, out record);

        /// <summary>
        /// Save the records to ApplicationData.Current.LocalSettings.
        /// You have to call this before application suspension
        /// to make records persistent.
        /// </summary>
        public void Save()
        {
            var recordsArr = records.Values.ToArray();
            string persisString = JsonConvert.SerializeObject(recordsArr);
            ApplicationData.Current.LocalSettings.Values[SettingKey] = persisString;
        }

        private void LoadRecords(int capacity)
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(
                    SettingKey, out object output) && output is string outputString)
                {
                    var thisVersion = Package.Current.Id.Version;
                    var recordsArr = JsonConvert
                        .DeserializeObject<ActivationRecord[]>(outputString);
                    var availableRecords = recordsArr.OrderByDescending(
                        rec => rec.LastActivationTime).Take(capacity);
                    foreach (var record in availableRecords)
                        records.Add(record.Key, record);
                }
            }
            catch (Exception) { }
        }

        private static string SettingKey => nameof(PersistentActionManager);
    }
}
