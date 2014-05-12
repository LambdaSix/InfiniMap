using System;
using System.Collections.Generic;

namespace InfiniMap.Test
{
    #region Test Helpers

    internal class EqualityLambda<T> : EqualityComparer<T>
    {
        private readonly Func<T, T, bool> _comparer;

        public EqualityLambda(Func<T, T, bool> comparer)
        {
            _comparer = comparer;
        }

        public override bool Equals(T x, T y)
        {
            return _comparer(x, y);
        }

        public override int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }

    internal struct StructItem
    {
        public int ItemId;
    }

    internal class ClassItem
    {
        public int ItemId;
    }

    #endregion
}