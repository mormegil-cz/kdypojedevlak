using System;
using System.Collections.Generic;

namespace KdyPojedeVlak.Engine
{
    public static class LinqExtensions
    {
        public static void IntoDictionary<TSource, TKey, TValue>(
            this IEnumerable<TSource> source, IDictionary<TKey, TValue> destination,
            Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            foreach(var item in source)
            {
                destination.Add(keySelector(item), valueSelector(item));
            }
        }
    }
}
