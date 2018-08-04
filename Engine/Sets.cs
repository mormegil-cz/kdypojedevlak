using System;
using System.Collections.Generic;

namespace KdyPojedeVlak.Engine
{
    public static class Sets<T>
    {
        public static readonly ISet<T> Empty = new HashSet<T>(0);
        public static readonly SortedSet<T> EmptySortedSet = new SortedSet<T>();
    }
}