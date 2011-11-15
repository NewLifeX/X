using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime;
using NewLife.Reflection;

namespace NewLife.Linq
{
    internal abstract class OrderedEnumerable<TElement> : IOrderedEnumerable<TElement>, IEnumerable<TElement>, IEnumerable
    {
        internal IEnumerable<TElement> source;
        public IEnumerator<TElement> GetEnumerator()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(this.source);
            if (buffer.count > 0)
            {
                EnumerableSorter<TElement> enumerableSorter = this.GetEnumerableSorter(null);
                int[] array = enumerableSorter.Sort(buffer.items, buffer.count);
                enumerableSorter = null;
                for (int i = 0; i < buffer.count; i++)
                {
                    yield return buffer.items[array[i]];
                }
            }
            yield break;
        }
        internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next);
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            return new OrderedEnumerable<TElement, TKey>(this.source, keySelector, comparer, descending)
            {
                parent = this
            };
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        protected OrderedEnumerable()
        {
        }
    }

    internal class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
    {
        internal OrderedEnumerable<TElement> parent;
        internal Func<TElement, TKey> keySelector;
        internal IComparer<TKey> comparer;
        internal bool descending;
        internal OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            this.source = source;
            this.parent = null;
            this.keySelector = keySelector;
            this.comparer = ((comparer != null) ? comparer : Comparer<TKey>.Default);
            this.descending = descending;
        }
        internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
        {
            EnumerableSorter<TElement> enumerableSorter = new EnumerableSorter<TElement, TKey>(this.keySelector, this.comparer, this.descending, next);
            if (this.parent != null)
            {
                enumerableSorter = this.parent.GetEnumerableSorter(enumerableSorter);
            }
            return enumerableSorter;
        }
    }
}