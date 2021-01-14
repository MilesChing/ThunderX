using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TX.Core.Models.Progresses
{
    /// <summary>
    /// Range is a closed interval of two integers [From, To] .
    /// </summary>
    public struct Range<T> where T : struct, IComparable 
    {
        /// <summary>
        /// Initialize a range with two boundaries.
        /// </summary>
        /// <param name="from">Lower bound of the range.</param>
        /// <param name="to">Upper bound of the range.</param>
        public Range(T from, T to)
        {
            if (from.CompareTo(to) > 0) throw new ArgumentException(
                "Given lower bound is bigger than given upper bound.");
            From = from;
            To = to;
        }

        /// <summary>
        /// Lower bound of the range.
        /// </summary>
        public T From { get; private set; }
        /// <summary>
        /// Upper bound of the range.
        /// </summary>
        public T To { get; private set; }

        /// <summary>
        /// Union the range with given range to get a new range. 
        /// There is always one single range returned, it will be the smallest range
        /// which covers both given ranges.
        /// </summary>
        /// <param name="range">Given range.</param>
        /// <returns>The range returned.</returns>
        public Range<T> Union(Range<T> range)
            => new Range<T>(
                   From.CompareTo(range.From) < 0 ? From : range.From,
                   To.CompareTo(range.To) > 0 ? To : range.To
               );

        /// <summary>
        /// Return whether the range intersects with another.
        /// </summary>
        /// <param name="range">Given range.</param>
        /// <returns>Whether the range intersects with another.</returns>
        public bool IsIntersect(Range<T> range)
        {
            if (From.CompareTo(range.From) == 0)
                return true;
            else if (From.CompareTo(range.From) < 0)
                return To.CompareTo(range.From) > 0;
            else
                return From.CompareTo(range.To) < 0;
        }
    }
}
