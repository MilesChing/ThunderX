using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace TX.Utils
{
    public static class VisFuncs
    {
        public static bool Inverse(bool boolean) => !boolean;

        public static Visibility BooleanToVisibility(bool boolean, bool inverse = false) =>
            (boolean ^ inverse) ? Visibility.Visible : Visibility.Collapsed;

        public static Visibility ObjectToVisibility(object obj, bool inverse = false) =>
            BooleanToVisibility(!IsNull(obj), inverse);

        public static Visibility IntegerToVisibility(int integer, bool inverse = false) =>
            BooleanToVisibility(integer != 0, inverse);

        public static string FormatDateTime(DateTime dateTime, string format) => dateTime.ToString(format);

        public static string FormatTimeSpan(TimeSpan timeSpan, string format) => timeSpan.ToString(format);

        public static bool IsNull(object obj, bool inverse = false) => (obj == null) ^ inverse;
    }
}
