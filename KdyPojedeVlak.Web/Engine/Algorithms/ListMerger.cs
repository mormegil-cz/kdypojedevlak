using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KdyPojedeVlak.Web.Engine.Algorithms
{
    public static class ListMerger
    {
        public static List<T> MergeLists<T>(List<List<T>> lists)
        {
            return Implementation<T>.MergeLists(lists);
        }

        private static class Implementation<T>
        {
            private static readonly ListSection<T> emptyList = new(new List<T>(0), 0, 0);

            public static List<T> MergeLists(List<List<T>> lists)
            {
                ListSection<T> result = emptyList;
                foreach (var list in lists)
                {
                    result = MergeSingle(new ListSection<T>(list), result);
                }
                return result.ToList();
            }

            private static ListSection<T> MergeSingle(ListSection<T> left, ListSection<T> right)
            {
                if (left.Count == 0) return right;
                if (right.Count == 0) return left;

                var (leftHead, rightHead, leftTail, rightTail) = FindCommon(left, right);

                return leftHead.Concat(rightHead).Concat(MergeSingle(leftTail, rightTail));
            }

            private static (ListSection<T>, ListSection<T>, ListSection<T>, ListSection<T>) FindCommon(ListSection<T> left, ListSection<T> right)
            {
                if (left.Count == 0) return (emptyList, right, emptyList, emptyList);
                if (right.Count == 0) return (left, emptyList, emptyList, emptyList);

                for (var i = 0; i < left.Count; ++i)
                {
                    var commonPos = right.IndexOf(left[i]);
                    if (commonPos >= 0)
                    {
                        return (left.Section(0, i), right.Section(0, commonPos + 1), left.Section(i + 1), right.Section(commonPos + 1));
                    }
                }

                return (left, right, emptyList, emptyList);
            }
        }

        private struct ListSection<T> : IReadOnlyCollection<T>
        {
            private readonly List<T> list;
            private readonly int start;
            private readonly int end;

            public int Count => end - start;

            public ListSection(List<T> list)
            {
                this.list = list;
                this.start = 0;
                this.end = list.Count;
            }

            public ListSection(List<T> list, int start, int end)
            {
                this.list = list;
                this.start = start;
                this.end = end;
            }

            public ListSection<T> Concat(ListSection<T> other)
            {
                if (other.Count == 0) return this;
                if (Count == 0) return other;

                return other.list == list && other.start == end
                    ? new ListSection<T>(list, start, other.end)
                    : new ListSection<T>(Enumerable.Concat(this, other).ToList());
            }

            public int IndexOf(T item)
            {
                return list.IndexOf(item, start, Count) - start;
            }

            public ListSection<T> Section(int innerStart) => Section(innerStart, Count);

            public ListSection<T> Section(int innerStart, int innerEnd)
            {
                if (innerStart < 0) throw new ArgumentOutOfRangeException(nameof(innerStart), innerStart, "Start cannot be negative");
                if (innerStart > innerEnd) throw new ArgumentOutOfRangeException(nameof(innerEnd), innerEnd, "End must not be before start");
                if (innerEnd > Count) throw new ArgumentOutOfRangeException(nameof(innerEnd), innerEnd, String.Format("End comes after the end of the {0}-element section", Count));

                return new ListSection<T>(list, this.start + innerStart, this.start + innerEnd);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return list.Skip(start).Take(Count).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public T this[int i] =>
                i < 0 || i >= Count
                    ? throw new IndexOutOfRangeException(String.Format("Cannot index {0} at a {1}-element section", i, Count))
                    : list[start + i];
        }
    }
}