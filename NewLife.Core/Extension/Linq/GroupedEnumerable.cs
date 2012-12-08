#if !NET4
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using NewLife.Reflection;

namespace NewLife.Linq
{
    internal class GroupedEnumerable<TSource, TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
    {
        private IEnumerable<TSource> source;
        private Func<TSource, TKey> keySelector;
        private Func<TSource, TElement> elementSelector;
        private IEqualityComparer<TKey> comparer;
        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
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
            this.source = source;
            this.keySelector = keySelector;
            this.elementSelector = elementSelector;
            this.comparer = comparer;
        }
        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            return Lookup<TKey, TElement>.Create<TSource>(this.source, this.keySelector, this.elementSelector, this.comparer).GetEnumerator();
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class GroupedEnumerable<TSource, TKey, TElement, TResult> : IEnumerable<TResult>, IEnumerable
    {
        private IEnumerable<TSource> source;
        private Func<TSource, TKey> keySelector;
        private Func<TSource, TElement> elementSelector;
        private IEqualityComparer<TKey> comparer;
        private Func<TKey, IEnumerable<TElement>, TResult> resultSelector;
        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
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
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            this.source = source;
            this.keySelector = keySelector;
            this.elementSelector = elementSelector;
            this.comparer = comparer;
            this.resultSelector = resultSelector;
        }
        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TElement> lookup = Lookup<TKey, TElement>.Create<TSource>(this.source, this.keySelector, this.elementSelector, this.comparer);
            return lookup.ApplyResultSelector<TResult>(this.resultSelector).GetEnumerator();
        }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
#endif