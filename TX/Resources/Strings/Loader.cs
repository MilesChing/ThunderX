using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace TX.Resources.Strings
{
    public static class Loader
    {
        public static string Get(string key) => resourceLoader.GetString(key);

        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
    }
}
