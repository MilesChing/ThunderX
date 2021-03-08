using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Utils;

namespace TX.Core.Models.Sources
{
    class ThunderSource : AbstractSource, ISingleSubsourceExtracted
    {
        public ThunderSource(Uri uri) : base(uri) { }

        public static bool IsValid(Uri uri) => uri.Scheme.Equals("thunder");

        public Task<AbstractSource> GetSubsourceAsync() =>
            Task.Run(() =>  CreateSource(Uri.DecodeThunderUri()));
    }
}
