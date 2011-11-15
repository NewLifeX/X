using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using NewLife.Reflection;

namespace NewLife.Linq
{
    /// <summary>表示映射到一个或多个值的各个键的集合。</summary>
    /// <typeparam name="TKey">
    ///   <see cref="T:NewLife.Linq.Lookup`2" /> 中的键的类型。</typeparam>
    /// <typeparam name="TElement">
    ///   <see cref="T:NewLife.Linq.Lookup`2" /> 中的每个 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 值的元素的类型。</typeparam>
    /// <filterpriority>2</filterpriority>
    public class Lookup<TKey, TElement> : ILookup<TKey, TElement>, IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
    {
        internal class Grouping : IGrouping<TKey, TElement>, IList<TElement>, ICollection<TElement>, IEnumerable<TElement>, IEnumerable
        {
            internal TKey key;
            internal int hashCode;
            internal TElement[] elements;
            internal int count;
            internal Lookup<TKey, TElement>.Grouping hashNext;
            internal Lookup<TKey, TElement>.Grouping next;
            public TKey Key
            {
                [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
                get
                {
                    return this.key;
                }
            }
            int ICollection<TElement>.Count
            {
                get
                {
                    return this.count;
                }
            }
            bool ICollection<TElement>.IsReadOnly
            {
                get
                {
                    return true;
                }
            }
            TElement IList<TElement>.this[int index]
            {
                get
                {
                    if (index < 0 || index >= this.count)
                    {
                        throw new ArgumentOutOfRangeException("index");
                    }
                    return this.elements[index];
                }
                set
                {
                    throw new NotSupportedException();
                }
            }
            internal void Add(TElement element)
            {
                if (this.elements.Length == this.count)
                {
                    Array.Resize<TElement>(ref this.elements, checked(this.count * 2));
                }
                this.elements[this.count] = element;
                this.count++;
            }
            public IEnumerator<TElement> GetEnumerator()
            {
                for (int i = 0; i < this.count; i++)
                {
                    yield return this.elements[i];
                }
                yield break;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
            void ICollection<TElement>.Add(TElement item)
            {
                throw new NotSupportedException();
            }
            void ICollection<TElement>.Clear()
            {
                throw new NotSupportedException();
            }
            bool ICollection<TElement>.Contains(TElement item)
            {
                return Array.IndexOf<TElement>(this.elements, item, 0, this.count) >= 0;
            }
            void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
            {
                Array.Copy(this.elements, 0, array, arrayIndex, this.count);
            }
            bool ICollection<TElement>.Remove(TElement item)
            {
                throw new NotSupportedException();
            }
            int IList<TElement>.IndexOf(TElement item)
            {
                return Array.IndexOf<TElement>(this.elements, item, 0, this.count);
            }
            void IList<TElement>.Insert(int index, TElement item)
            {
                throw new NotSupportedException();
            }
            void IList<TElement>.RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public Grouping()
            {
            }
        }
        private IEqualityComparer<TKey> comparer;
        private Lookup<TKey, TElement>.Grouping[] groupings;
        private Lookup<TKey, TElement>.Grouping lastGrouping;
        private int count;
        /// <summary>获取 <see cref="T:NewLife.Linq.Lookup`2" /> 中的键/值对集合的数目。</summary>
        /// <returns>
        ///   <see cref="T:NewLife.Linq.Lookup`2" /> 中键/值对集合的数目。</returns>
        public int Count
        {
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            get
            {
                return this.count;
            }
        }
        /// <summary>获取按指定键进行索引的值的集合。</summary>
        /// <returns>按指定键进行索引的值的集合。</returns>
        /// <param name="key">所需值集合的键。</param>
        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                Lookup<TKey, TElement>.Grouping grouping = this.GetGrouping(key, false);
                if (grouping != null)
                {
                    return grouping;
                }
                return EmptyEnumerable<TElement>.Instance;
            }
        }
        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            if (elementSelector == null)
            {
                throw new ArgumentNullException("elementSelector");
            }
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TSource current in source)
            {
                lookup.GetGrouping(keySelector(current), true).Add(elementSelector(current));
            }
            return lookup;
        }
        internal static Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
            foreach (TElement current in source)
            {
                TKey tKey = keySelector(current);
                if (tKey != null)
                {
                    lookup.GetGrouping(tKey, true).Add(current);
                }
            }
            return lookup;
        }
        private Lookup(IEqualityComparer<TKey> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }
            this.comparer = comparer;
            this.groupings = new Lookup<TKey, TElement>.Grouping[7];
        }
        /// <summary>确定指定的键是否位于 <see cref="T:NewLife.Linq.Lookup`2" /> 中。</summary>
        /// <returns>如果 <paramref name="key" /> 在 <see cref="T:NewLife.Linq.Lookup`2" /> 中，则为 true；否则为 false。</returns>
        /// <param name="key">要在 <see cref="T:NewLife.Linq.Lookup`2" /> 中查找的键。</param>
        public bool Contains(TKey key)
        {
            return this.GetGrouping(key, false) != null;
        }
        /// <summary>返回循环访问 <see cref="T:NewLife.Linq.Lookup`2" /> 的泛型枚举数。</summary>
        /// <returns>
        ///   <see cref="T:NewLife.Linq.Lookup`2" /> 的枚举数。</returns>
        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            Lookup<TKey, TElement>.Grouping next = this.lastGrouping;
            if (next != null)
            {
                do
                {
                    next = next.next;
                    yield return next;
                }
                while (next != this.lastGrouping);
            }
            yield break;
        }
        /// <summary>对每个键及其关联值应用转换函数，并返回结果。</summary>
        /// <returns>包含 <see cref="T:NewLife.Linq.Lookup`2" /> 中的各个键/值对集合中的一个值的集合。</returns>
        /// <param name="resultSelector">从每个键及其关联值投影结果值的函数。</param>
        /// <typeparam name="TResult">
        ///   <paramref name="resultSelector" /> 生成的结果值的类型。</typeparam>
        /// <filterpriority>2</filterpriority>
        public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            Lookup<TKey, TElement>.Grouping next = this.lastGrouping;
            if (next != null)
            {
                do
                {
                    next = next.next;
                    if (next.count != next.elements.Length)
                    {
                        Array.Resize<TElement>(ref next.elements, next.count);
                    }
                    yield return resultSelector(next.key, next.elements);
                }
                while (next != this.lastGrouping);
            }
            yield break;
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        internal int InternalGetHashCode(TKey key)
        {
            if (key != null)
            {
                return this.comparer.GetHashCode(key) & 2147483647;
            }
            return 0;
        }
        internal Lookup<TKey, TElement>.Grouping GetGrouping(TKey key, bool create)
        {
            int num = this.InternalGetHashCode(key);
            for (Lookup<TKey, TElement>.Grouping grouping = this.groupings[num % this.groupings.Length]; grouping != null; grouping = grouping.hashNext)
            {
                if (grouping.hashCode == num && this.comparer.Equals(grouping.key, key))
                {
                    return grouping;
                }
            }
            if (create)
            {
                if (this.count == this.groupings.Length)
                {
                    this.Resize();
                }
                int num2 = num % this.groupings.Length;
                Lookup<TKey, TElement>.Grouping grouping2 = new Lookup<TKey, TElement>.Grouping();
                grouping2.key = key;
                grouping2.hashCode = num;
                grouping2.elements = new TElement[1];
                grouping2.hashNext = this.groupings[num2];
                this.groupings[num2] = grouping2;
                if (this.lastGrouping == null)
                {
                    grouping2.next = grouping2;
                }
                else
                {
                    grouping2.next = this.lastGrouping.next;
                    this.lastGrouping.next = grouping2;
                }
                this.lastGrouping = grouping2;
                this.count++;
                return grouping2;
            }
            return null;
        }
        private void Resize()
        {
            int num = checked(this.count * 2 + 1);
            Lookup<TKey, TElement>.Grouping[] array = new Lookup<TKey, TElement>.Grouping[num];
            Lookup<TKey, TElement>.Grouping next = this.lastGrouping;
            do
            {
                next = next.next;
                int num2 = next.hashCode % num;
                next.hashNext = array[num2];
                array[num2] = next;
            }
            while (next != this.lastGrouping);
            this.groupings = array;
        }
    }
}