using System.Collections.Generic;

namespace KdyPojedeVlak.Engine
{
    public static class Sets<T>
    {
        public static readonly HashSet<T> Empty = new(0);
        public static readonly SortedSet<T> EmptySortedSet = new();
    }

    public static class SetsExtensions
    {
        public static void AddAll<T>(this ISet<T> set, IEnumerable<T> elems)
        {
            foreach (var elem in elems)
            {
                set.Add(elem);
            }
        }
    }
}