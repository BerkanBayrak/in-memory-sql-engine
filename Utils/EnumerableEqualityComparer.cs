using System.Collections.Generic;
using System.Linq;

namespace SqlEngine.Utils
{
    // Equality comparer for sequences, compares elements in order
    public class EnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly IEqualityComparer<T> _elementComparer;

        public EnumerableEqualityComparer() : this(EqualityComparer<T>.Default) { }

        public EnumerableEqualityComparer(IEqualityComparer<T> elementComparer)
        {
            _elementComparer = elementComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.SequenceEqual(y, _elementComparer);
        }

        public int GetHashCode(IEnumerable<T> obj)
        {
            if (obj is null) return 0;
            unchecked
            {
                int hash = 17;
                foreach (var element in obj)
                    hash = hash * 31 + (_elementComparer.GetHashCode(element) & 0x7FFFFFFF);
                return hash;
            }
        }
    }
}
