using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace TX.Utils
{
    public static class PackageVersionExtensions
    {
        public static string ToString(PackageVersion version)
        {
            return string.Join(".",
                Package.Current.Id.Version.Major,
                Package.Current.Id.Version.Minor,
                Package.Current.Id.Version.Build,
                Package.Current.Id.Version.Revision);
        }

        public static string CurrentVersionString =>
            ToString(Package.Current.Id.Version);

        public static int CompareTo(this PackageVersion p, PackageVersion v)
        {
            if (p.Major == v.Major)
                if (p.Minor == v.Minor)
                    if (p.Build == v.Build)
                        return p.Revision.CompareTo(v.Revision);
                    else return p.Build.CompareTo(v.Build);
                else return p.Minor.CompareTo(v.Minor);
            else return p.Major.CompareTo(v.Major);
        }
    }
}
