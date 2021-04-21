#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace KdyPojedeVlak.Engine
{
    public static class LinqExtensions
    {
        public static void IntoDictionary<TSource, TKey, TValue>(
            this IEnumerable<TSource> source, IDictionary<TKey, TValue> destination,
            Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (destination.ContainsKey(key))
                {
                    // WTF?
                    DebugLog.LogProblem($"Duplicate key '{key}' when preparing dictionary");
                }
                else
                {
                    destination.Add(keySelector(item), valueSelector(item));
                }
            }
        }

        public static IDictionary<TKey, TValue> ToDictionaryLax<TSource, TKey, TValue>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        {
            var destination = new Dictionary<TKey, TValue>();
            IntoDictionary(source, destination, keySelector, valueSelector);
            return destination;
        }

        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
        {
            foreach (var key in keys) dictionary.Remove(key);
        }

        public static IEnumerable<T> ConcatExisting<T>(params IEnumerable<T>?[] sequences)
        {
            return sequences.Where(s => s != null).SelectMany(s => s!);
        }
    }
}