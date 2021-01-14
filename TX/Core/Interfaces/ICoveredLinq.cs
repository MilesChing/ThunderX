using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Progresses;

namespace TX.Core.Interfaces
{
    public interface ICoveredLinq
    {
        IEnumerable<KeyValuePair<Range<long>, bool>> GetRangeAndDownloadingStatus();
    }
}
