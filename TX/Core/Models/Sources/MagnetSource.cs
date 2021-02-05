using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Targets;
using MonoTorrent;

namespace TX.Core.Models.Sources
{
    class MagnetSource : AbstractSource, ISingleTargetExtracted
    {
        public MagnetSource(Uri uri) : base(uri)
        {
            Ensure.That(uri.Scheme).Is("magnet");
            link = MagnetLink.FromUri(Uri);
        }

        public Task<AbstractTarget> GetTargetAsync() =>
            Task.Run<AbstractTarget>(() => new MagnetTarget(link));

        private readonly MagnetLink link = null;
    }
}
