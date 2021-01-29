using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses
{
    struct Range
    {
        public Range(long begin, long end)
        {
            Ensure.That(begin).IsLte(end);
            Begin = begin;
            End = end;
        }

        public long Length => End - Begin;

        public Range Intersect(Range range)
            => new Range(Math.Max(Begin, range.Begin), Math.Min(End, range.End));

        public Range Union(Range range)
            => new Range(Math.Min(Begin, range.Begin), Math.Max(End, range.End));

        public bool IsIntersectWith(Range range)
            => Math.Max(Begin, range.Begin) < Math.Min(End, range.End);

        public bool Equals(Range range)
            => (Begin == range.Begin) && (End == range.End);

        public bool Contains(Range range) 
            => (Begin <= range.Begin) && (End >= range.End);

        public IEnumerable<Range> Except(Range range)
        {
            range = Intersect(range);
            if (range.Length == 0) yield return this;
            else
            {
                if (range.Begin > Begin) yield return new Range(Begin, range.Begin);
                if (range.End < End) yield return new Range(range.End, End);
            }
        }

        public long Begin;
        public long End;
    }
}
