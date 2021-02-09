using MonoTorrent;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Targets
{
    class MagnetTarget : AbstractTarget
    {
        public MagnetTarget(Uri linkUri)
        {
            link = MagnetLink.FromUri(linkUri);
            LinkUri = linkUri;
        }

        [JsonIgnore]
        public MagnetLink Link => link;

        public Uri LinkUri { get; private set; }

        protected override string GetSuggestedName() => link.Name;

        private MagnetLink link = null;
    }
}
