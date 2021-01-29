using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Progresses.Interfaces;

namespace TX.Core.Interfaces
{
    public interface IVisibleProgress
    {
        IReadOnlyList<IVisibleRange> VisibleRangeList { get; }

        event Action<IVisibleProgress> VisibleRangeListChanged;
    }

    public interface IVisibleRange : INotifyPropertyChanged
    {
        float Progress { get; }

        float Total { get; }
    }
}
