using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Collections
{
    public interface ISyncableEnumerable<T> :
        IEnumerable<T>, INotifyCollectionChanged { }
}
