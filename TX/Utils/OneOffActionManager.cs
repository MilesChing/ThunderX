using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace TX.Utils
{
    public class OneOffActionManager
    {
        public OneOffActionManager()
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(
                SettingKey, out object output) && output is string outputString)
            {
                var subStrings = outputString.Split(Separator);
                if (subStrings.Length >= 2)
                {
                    string storageVersion = subStrings[0];
                    if (storageVersion.Equals(ToString(Package.Current.Id.Version)))
                        completedNames.AddRange(subStrings.Skip(1));
                }
            }
        }

        public bool Try(string name, Action task)
        {
            if (completedNames.Contains(name))
                return false;
            completedNames.Add(name);
            task?.Invoke();
            return true;
        }

        public void SaveToStorage()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendJoin(
                '$', completedNames.Prepend(
                    ToString(Package.Current.Id.Version)));
            ApplicationData.Current.LocalSettings.Values[
                SettingKey] = stringBuilder.ToString();
        }

        private string ToString(PackageVersion id) =>
            $"{id.Major}_{id.Minor}_{id.Build}_{id.Revision}";

        private readonly List<string> completedNames = new List<string>();
        private readonly static string SettingKey = "OneOffActionStorage";
        private readonly static char Separator = '$';
    }
}
