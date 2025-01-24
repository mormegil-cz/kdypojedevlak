using System.Collections.Generic;

namespace KdyPojedeVlak.Web.Engine;

public static class Sets<T>
{
    public static readonly HashSet<T> Empty = [];
    public static readonly SortedSet<T> EmptySortedSet = [];
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