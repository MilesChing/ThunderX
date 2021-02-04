using MonoTorrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Targets
{
    class MagnetTarget : AbstractTarget
    {
        public MagnetTarget(MagnetLink link)
        {
            this.link = link;
        }

        public MagnetLink Link => link;

        protected override string GetSuggestedName() => link.Name;

        private MagnetLink link = null;
    }
}
