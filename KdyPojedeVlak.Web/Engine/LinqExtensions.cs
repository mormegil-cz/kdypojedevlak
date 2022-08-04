#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace KdyPojedeVlak.Web.Engine
{
    public static class LinqExtensions
    {
        public static void IntoDictionary<TSource, TKey, TValue>(
            this IEnumerable<TSource> source, IDictionary<TKey, TValue> destination,
            Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
            where TKey : notnull
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
            where TKey : notnull
        {
            var destination = new Dictionary<TKey, TValue>();
            IntoDictionary(source, destination, keySelector, valueSelector);
            return destination;
        }

        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<TKey> keys)
            where TKey : notnull
        {
            foreach (var key in keys) dictionary.Remove(key);
        }

        public static IEnumerable<T> ConcatExisting<T>(params IEnumerable<T>?[] sequences) => sequences.Where(s => s != null).SelectMany(s => s!);

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence)
            where T : notnull
            => (IEnumerable<T>)sequence.Where(x => x != null);
    }
}