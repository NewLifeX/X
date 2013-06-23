#if !NET4
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Threading;
using NewLife.Reflection;

#pragma warning disable 1734
namespace NewLife.Linq
{
    /// <summary>提供一组用于查询实现 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 的对象的 static（在 Visual Basic 中为 Shared）方法。</summary>
    public static partial class Enumerable
    {
        #region 内部类
        private abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerable, IEnumerator<TSource>, IDisposable, IEnumerator
        {
            private int threadId;
            internal int state;
            internal TSource current;
            public TSource Current
            {
                [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
                get
                {
                    return this.current;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }
            public Iterator()
            {
                this.threadId = Thread.CurrentThread.ManagedThreadId;
            }
            public abstract Enumerable.Iterator<TSource> Clone();
            public virtual void Dispose()
            {
                this.current = default(TSource);
                this.state = -1;
            }
            public IEnumerator<TSource> GetEnumerator()
            {
                if (this.threadId == Thread.CurrentThread.ManagedThreadId && this.state == 0)
                {
                    this.state = 1;
                    return this;
                }
                Enumerable.Iterator<TSource> iterator = this.Clone();
                iterator.state = 1;
                return iterator;
            }
            public abstract bool MoveNext();
            public abstract IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector);
            public abstract IEnumerable<TSource> Where(Func<TSource, bool> predicate);
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
            void IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }
        }
        private class WhereEnumerableIterator<TSource> : Enumerable.Iterator<TSource>
        {
            private IEnumerable<TSource> source;
            private Func<TSource, bool> predicate;
            private IEnumerator<TSource> enumerator;
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }
            public override Enumerable.Iterator<TSource> Clone()
            {
                return new Enumerable.WhereEnumerableIterator<TSource>(this.source, this.predicate);
            }
            public override void Dispose()
            {
                if (this.enumerator != null)
                {
                    this.enumerator.Dispose();
                }
                this.enumerator = null;
                base.Dispose();
            }
            public override bool MoveNext()
            {
                switch (this.state)
                {
                    case 1:
                        {
                            this.enumerator = this.source.GetEnumerator();
                            this.state = 2;
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
                while (this.enumerator.MoveNext())
                {
                    TSource current = this.enumerator.Current;
                    if (this.predicate(current))
                    {
                        this.current = current;
                        return true;
                    }
                }
                this.Dispose();
                return false;
            }
            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new Enumerable.WhereSelectEnumerableIterator<TSource, TResult>(this.source, this.predicate, selector);
            }
            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new Enumerable.WhereEnumerableIterator<TSource>(this.source, Enumerable.CombinePredicates<TSource>(this.predicate, predicate));
            }
        }
        private class WhereArrayIterator<TSource> : Enumerable.Iterator<TSource>
        {
            private TSource[] source;
            private Func<TSource, bool> predicate;
            private int index;
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }
            public override Enumerable.Iterator<TSource> Clone()
            {
                return new Enumerable.WhereArrayIterator<TSource>(this.source, this.predicate);
            }
            public override bool MoveNext()
            {
                if (this.state == 1)
                {
                    while (this.index < this.source.Length)
                    {
                        TSource tSource = this.source[this.index];
                        this.index++;
                        if (this.predicate(tSource))
                        {
                            this.current = tSource;
                            return true;
                        }
                    }
                    this.Dispose();
                }
                return false;
            }
            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new Enumerable.WhereSelectArrayIterator<TSource, TResult>(this.source, this.predicate, selector);
            }
            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new Enumerable.WhereArrayIterator<TSource>(this.source, Enumerable.CombinePredicates<TSource>(this.predicate, predicate));
            }
        }
        private class WhereListIterator<TSource> : Enumerable.Iterator<TSource>
        {
            private List<TSource> source;
            private Func<TSource, bool> predicate;
            private List<TSource>.Enumerator enumerator;
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
            {
                this.source = source;
                this.predicate = predicate;
            }
            public override Enumerable.Iterator<TSource> Clone()
            {
                return new Enumerable.WhereListIterator<TSource>(this.source, this.predicate);
            }
            public override bool MoveNext()
            {
                switch (this.state)
                {
                    case 1:
                        {
                            this.enumerator = this.source.GetEnumerator();
                            this.state = 2;
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
                while (this.enumerator.MoveNext())
                {
                    TSource current = this.enumerator.Current;
                    if (this.predicate(current))
                    {
                        this.current = current;
                        return true;
                    }
                }
                this.Dispose();
                return false;
            }
            public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
            {
                return new Enumerable.WhereSelectListIterator<TSource, TResult>(this.source, this.predicate, selector);
            }
            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
            {
                return new Enumerable.WhereListIterator<TSource>(this.source, Enumerable.CombinePredicates<TSource>(this.predicate, predicate));
            }
        }
        private class WhereSelectEnumerableIterator<TSource, TResult> : Enumerable.Iterator<TResult>
        {
            private IEnumerable<TSource> source;
            private Func<TSource, bool> predicate;
            private Func<TSource, TResult> selector;
            private IEnumerator<TSource> enumerator;
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public WhereSelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                this.source = source;
                this.predicate = predicate;
                this.selector = selector;
            }
            public override Enumerable.Iterator<TResult> Clone()
            {
                return new Enumerable.WhereSelectEnumerableIterator<TSource, TResult>(this.source, this.predicate, this.selector);
            }
            public override void Dispose()
            {
                if (this.enumerator != null)
                {
                    this.enumerator.Dispose();
                }
                this.enumerator = null;
                base.Dispose();
            }
            public override bool MoveNext()
            {
                switch (this.state)
                {
                    case 1:
                        {
                            this.enumerator = this.source.GetEnumerator();
                            this.state = 2;
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
                while (this.enumerator.MoveNext())
                {
                    TSource current = this.enumerator.Current;
                    if (this.predicate == null || this.predicate(current))
                    {
                        this.current = this.selector(current);
                        return true;
                    }
                }
                this.Dispose();
                return false;
            }
            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new Enumerable.WhereSelectEnumerableIterator<TSource, TResult2>(this.source, this.predicate, Enumerable.CombineSelectors<TSource, TResult, TResult2>(this.selector, selector));
            }
            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new Enumerable.WhereEnumerableIterator<TResult>(this, predicate);
            }
        }
        private class WhereSelectArrayIterator<TSource, TResult> : Enumerable.Iterator<TResult>
        {
            private TSource[] source;
            private Func<TSource, bool> predicate;
            private Func<TSource, TResult> selector;
            private int index;
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                this.source = source;
                this.predicate = predicate;
                this.selector = selector;
            }
            public override Enumerable.Iterator<TResult> Clone()
            {
                return new Enumerable.WhereSelectArrayIterator<TSource, TResult>(this.source, this.predicate, this.selector);
            }
            public override bool MoveNext()
            {
                if (this.state == 1)
                {
                    while (this.index < this.source.Length)
                    {
                        TSource arg = this.source[this.index];
                        this.index++;
                        if (this.predicate == null || this.predicate(arg))
                        {
                            this.current = this.selector(arg);
                            return true;
                        }
                    }
                    this.Dispose();
                }
                return false;
            }
            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new Enumerable.WhereSelectArrayIterator<TSource, TResult2>(this.source, this.predicate, Enumerable.CombineSelectors<TSource, TResult, TResult2>(this.selector, selector));
            }
            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new Enumerable.WhereEnumerableIterator<TResult>(this, predicate);
            }
        }
        private class WhereSelectListIterator<TSource, TResult> : Enumerable.Iterator<TResult>
        {
            private List<TSource> source;
            private Func<TSource, bool> predicate;
            private Func<TSource, TResult> selector;
            private List<TSource>.Enumerator enumerator;
            [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
            public WhereSelectListIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                this.source = source;
                this.predicate = predicate;
                this.selector = selector;
            }
            public override Enumerable.Iterator<TResult> Clone()
            {
                return new Enumerable.WhereSelectListIterator<TSource, TResult>(this.source, this.predicate, this.selector);
            }
            public override bool MoveNext()
            {
                switch (this.state)
                {
                    case 1:
                        {
                            this.enumerator = this.source.GetEnumerator();
                            this.state = 2;
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            return false;
                        }
                }
                while (this.enumerator.MoveNext())
                {
                    TSource current = this.enumerator.Current;
                    if (this.predicate == null || this.predicate(current))
                    {
                        this.current = this.selector(current);
                        return true;
                    }
                }
                this.Dispose();
                return false;
            }
            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new Enumerable.WhereSelectListIterator<TSource, TResult2>(this.source, this.predicate, Enumerable.CombineSelectors<TSource, TResult, TResult2>(this.selector, selector));
            }
            public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
            {
                return new Enumerable.WhereEnumerableIterator<TResult>(this, predicate);
            }
        }
        #endregion

        #region 集合查找
        /// <summary>基于谓词筛选值序列。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中满足条件的元素。</returns>
        /// <param name="source">要筛选的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            if (source is Enumerable.Iterator<TSource>)
            {
                return ((Enumerable.Iterator<TSource>)source).Where(predicate);
            }
            if (source is TSource[])
            {
                return new Enumerable.WhereArrayIterator<TSource>((TSource[])source, predicate);
            }
            if (source is List<TSource>)
            {
                return new Enumerable.WhereListIterator<TSource>((List<TSource>)source, predicate);
            }
            return new Enumerable.WhereEnumerableIterator<TSource>(source, predicate);
        }
        /// <summary>基于谓词筛选值序列。将在谓词函数的逻辑中使用每个元素的索引。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中满足条件的元素。</returns>
        /// <param name="source">要筛选的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个源元素是否满足条件的函数；该函数的第二个参数表示源元素的索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Enumerable.WhereIterator<TSource>(source, predicate);
        }
        private static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            checked
            {
                int num = -1;
                foreach (TSource current in source)
                {
                    num++;
                    if (predicate(current, num))
                    {
                        yield return current;
                    }
                }
                yield break;
            }
        }
        /// <summary>将序列中的每个元素投影到新表中。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素为对 <paramref name="source" /> 的每个元素调用转换函数的结果。</returns>
        /// <param name="source">一个值序列，要对该序列调用转换函数。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="selector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }
            if (source is Enumerable.Iterator<TSource>)
            {
                return ((Enumerable.Iterator<TSource>)source).Select<TResult>(selector);
            }
            if (source is TSource[])
            {
                return new Enumerable.WhereSelectArrayIterator<TSource, TResult>((TSource[])source, null, selector);
            }
            if (source is List<TSource>)
            {
                return new Enumerable.WhereSelectListIterator<TSource, TResult>((List<TSource>)source, null, selector);
            }
            return new Enumerable.WhereSelectEnumerableIterator<TSource, TResult>(source, null, selector);
        }
        /// <summary>通过合并元素的索引将序列的每个元素投影到新表中。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素为对 <paramref name="source" /> 的每个元素调用转换函数的结果。</returns>
        /// <param name="source">一个值序列，要对该序列调用转换函数。</param>
        /// <param name="selector">一个应用于每个源元素的转换函数；函数的第二个参数表示源元素的索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="selector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }
            return Enumerable.SelectIterator<TSource, TResult>(source, selector);
        }
        private static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            checked
            {
                int num = -1;
                foreach (TSource current in source)
                {
                    num++;
                    yield return selector(current, num);
                }
                yield break;
            }
        }
        private static Func<TSource, bool> CombinePredicates<TSource>(Func<TSource, bool> predicate1, Func<TSource, bool> predicate2)
        {
            return (TSource x) => predicate1(x) && predicate2(x);
        }
        private static Func<TSource, TResult> CombineSelectors<TSource, TMiddle, TResult>(Func<TSource, TMiddle> selector1, Func<TMiddle, TResult> selector2)
        {
            return (TSource x) => selector2(selector1(x));
        }
        /// <summary>将序列的每个元素投影到 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 并将结果序列合并为一个序列。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素是对输入序列的每个元素调用一对多转换函数的结果。</returns>
        /// <param name="source">一个要投影的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="selector" /> 返回的序列元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }
            return Enumerable.SelectManyIterator<TSource, TResult>(source, selector);
        }
        private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            foreach (TSource current in source)
            {
                foreach (TResult current2 in selector(current))
                {
                    yield return current2;
                }
            }
            yield break;
        }
        /// <summary>将序列的每个元素投影到 <see cref="T:System.Collections.Generic.IEnumerable`1" />，并将结果序列合并为一个序列。每个源元素的索引用于该元素的投影表。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素是对输入序列的每个元素调用一对多转换函数的结果。</returns>
        /// <param name="source">一个要投影的值序列。</param>
        /// <param name="selector">一个应用于每个源元素的转换函数；函数的第二个参数表示源元素的索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="selector" /> 返回的序列元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (selector == null)
            {
                throw new ArgumentNullException("selector");
            }
            return Enumerable.SelectManyIterator<TSource, TResult>(source, selector);
        }
        private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
        {
            checked
            {
                int num = -1;
                foreach (TSource current in source)
                {
                    num++;
                    foreach (TResult current2 in selector(current, num))
                    {
                        yield return current2;
                    }
                }
                yield break;
            }
        }
        /// <summary>将序列的每个元素投影到 <see cref="T:System.Collections.Generic.IEnumerable`1" />，并将结果序列合并为一个序列，并对其中每个元素调用结果选择器函数。每个源元素的索引用于该元素的中间投影表。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素是对 <paramref name="source" /> 的每个元素调用一对多转换函数 <paramref name="collectionSelector" />，然后将那些序列元素中的每一个及其相应的源元素映射为结果元素的结果。</returns>
        /// <param name="source">一个要投影的值序列。</param>
        /// <param name="collectionSelector">一个应用于每个源元素的转换函数；函数的第二个参数表示源元素的索引。</param>
        /// <param name="resultSelector">一个应用于中间序列的每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TCollection">
        ///   <paramref name="collectionSelector" /> 收集的中间元素的类型。</typeparam>
        /// <typeparam name="TResult">结果序列的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="collectionSelector" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (collectionSelector == null)
            {
                throw new ArgumentNullException("collectionSelector");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.SelectManyIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }
        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            checked
            {
                int num = -1;
                foreach (TSource current in source)
                {
                    num++;
                    foreach (TCollection current2 in collectionSelector(current, num))
                    {
                        yield return resultSelector(current, current2);
                    }
                }
                yield break;
            }
        }
        /// <summary>将序列的每个元素投影到 <see cref="T:System.Collections.Generic.IEnumerable`1" />，并将结果序列合并为一个序列，并对其中每个元素调用结果选择器函数。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素是对 <paramref name="source" /> 的每个元素调用一对多转换函数 <paramref name="collectionSelector" />，然后将那些序列元素中的每一个及其相应的源元素映射为结果元素的结果。</returns>
        /// <param name="source">一个要投影的值序列。</param>
        /// <param name="collectionSelector">一个应用于输入序列的每个元素的转换函数。</param>
        /// <param name="resultSelector">一个应用于中间序列的每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TCollection">
        ///   <paramref name="collectionSelector" /> 收集的中间元素的类型。</typeparam>
        /// <typeparam name="TResult">结果序列的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="collectionSelector" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (collectionSelector == null)
            {
                throw new ArgumentNullException("collectionSelector");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.SelectManyIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
        }
        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (TSource current in source)
            {
                foreach (TCollection current2 in collectionSelector(current))
                {
                    yield return resultSelector(current, current2);
                }
            }
            yield break;
        }
        /// <summary>从序列的开头返回指定数量的连续元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列开头的指定数量的元素。</returns>
        /// <param name="source">要从其返回元素的序列。</param>
        /// <param name="count">要返回的元素数量。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.TakeIterator<TSource>(source, count);
        }
        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            if (count > 0)
            {
                foreach (TSource current in source)
                {
                    yield return current;
                    if (--count == 0)
                    {
                        break;
                    }
                }
            }
            yield break;
        }
        /// <summary>只要满足指定的条件，就会返回序列的元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中出现在测试不再能够通过的元素之前的元素。</returns>
        /// <param name="source">要从其返回元素的序列。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Enumerable.TakeWhileIterator<TSource>(source, predicate);
        }
        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource current in source)
            {
                if (!predicate(current))
                {
                    break;
                }
                yield return current;
            }
            yield break;
        }
        /// <summary>只要满足指定的条件，就会返回序列的元素。将在谓词函数的逻辑中使用元素的索引。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中出现在测试不再能够通过的元素之前的元素。</returns>
        /// <param name="source">要从其返回元素的序列。</param>
        /// <param name="predicate">用于测试每个源元素是否满足条件的函数；该函数的第二个参数表示源元素的索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Enumerable.TakeWhileIterator<TSource>(source, predicate);
        }
        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            checked
            {
                int num = -1;
                foreach (TSource current in source)
                {
                    num++;
                    if (!predicate(current, num))
                    {
                        break;
                    }
                    yield return current;
                }
                yield break;
            }
        }
        /// <summary>跳过序列中指定数量的元素，然后返回剩余的元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中指定索引后出现的元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="count">返回剩余元素前要跳过的元素数量。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.SkipIterator<TSource>(source, count);
        }
        private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            //using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            //{
            //    while (count > 0 && enumerator.MoveNext())
            //    {
            //        count--;
            //    }
            //    if (count <= 0)
            //    {
            //        while (enumerator.MoveNext())
            //        {
            //            yield return enumerator.Current;
            //        }
            //    }
            //}
            //yield break;

            foreach (TSource item in source)
            {
                if (count-- <= 0) yield return item;
            }
        }
        /// <summary>只要满足指定的条件，就跳过序列中的元素，然后返回剩余元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中的元素，该输入序列从线性系列中没有通过 <paramref name="predicate" /> 指定测试的第一个元素开始。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Enumerable.SkipWhileIterator<TSource>(source, predicate);
        }
        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            bool flag = false;
            foreach (TSource current in source)
            {
                if (!flag && !predicate(current))
                {
                    flag = true;
                }
                if (flag)
                {
                    yield return current;
                }
            }
            yield break;
        }
        /// <summary>只要满足指定的条件，就跳过序列中的元素，然后返回剩余元素。将在谓词函数的逻辑中使用元素的索引。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含输入序列中的元素，该输入序列从线性系列中没有通过 <paramref name="predicate" /> 指定测试的第一个元素开始。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个源元素是否满足条件的函数；该函数的第二个参数表示源元素的索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static IEnumerable<TSource> SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return Enumerable.SkipWhileIterator<TSource>(source, predicate);
        }
        private static IEnumerable<TSource> SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            checked
            {
                int num = -1;
                bool flag = false;
                foreach (TSource current in source)
                {
                    num++;
                    if (!flag && !predicate(current, num))
                    {
                        flag = true;
                    }
                    if (flag)
                    {
                        yield return current;
                    }
                }
                yield break;
            }
        }
        #endregion

        #region 分组排序
        /// <summary>基于匹配键对两个序列的元素进行关联。使用默认的相等比较器对键进行比较。</summary>
        /// <returns>一个具有 <paramref name="TResult" /> 类型元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />，这些元素是通过对两个序列执行内部联接得来的。</returns>
        /// <param name="outer">要联接的第一个序列。</param>
        /// <param name="inner">要与第一个序列联接的序列。</param>
        /// <param name="outerKeySelector">用于从第一个序列的每个元素提取联接键的函数。</param>
        /// <param name="innerKeySelector">用于从第二个序列的每个元素提取联接键的函数。</param>
        /// <param name="resultSelector">用于从两个匹配元素创建结果元素的函数。</param>
        /// <typeparam name="TOuter">第一个序列中的元素的类型。</typeparam>
        /// <typeparam name="TInner">第二个序列中的元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <typeparam name="TResult">结果元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="outer" /> 或 <paramref name="inner" /> 或 <paramref name="outerKeySelector" /> 或 <paramref name="innerKeySelector" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
        {
            if (outer == null)
            {
                throw new ArgumentNullException("outer");
            }
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }
            if (outerKeySelector == null)
            {
                throw new ArgumentNullException("outerKeySelector");
            }
            if (innerKeySelector == null)
            {
                throw new ArgumentNullException("innerKeySelector");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.JoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }
        /// <summary>基于匹配键对两个序列的元素进行关联。使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 对键进行比较。</summary>
        /// <returns>一个具有 <paramref name="TResult" /> 类型元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />，这些元素是通过对两个序列执行内部联接得来的。</returns>
        /// <param name="outer">要联接的第一个序列。</param>
        /// <param name="inner">要与第一个序列联接的序列。</param>
        /// <param name="outerKeySelector">用于从第一个序列的每个元素提取联接键的函数。</param>
        /// <param name="innerKeySelector">用于从第二个序列的每个元素提取联接键的函数。</param>
        /// <param name="resultSelector">用于从两个匹配元素创建结果元素的函数。</param>
        /// <param name="comparer">一个 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />，用于对键进行哈希处理和比较。</param>
        /// <typeparam name="TOuter">第一个序列中的元素的类型。</typeparam>
        /// <typeparam name="TInner">第二个序列中的元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <typeparam name="TResult">结果元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="outer" /> 或 <paramref name="inner" /> 或 <paramref name="outerKeySelector" /> 或 <paramref name="innerKeySelector" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (outer == null)
            {
                throw new ArgumentNullException("outer");
            }
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }
            if (outerKeySelector == null)
            {
                throw new ArgumentNullException("outerKeySelector");
            }
            if (innerKeySelector == null)
            {
                throw new ArgumentNullException("innerKeySelector");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.JoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }
        private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
            foreach (TOuter current in outer)
            {
                Lookup<TKey, TInner>.Grouping grouping = lookup.GetGrouping(outerKeySelector(current), false);
                if (grouping != null)
                {
                    for (int i = 0; i < grouping.count; i++)
                    {
                        yield return resultSelector(current, grouping.elements[i]);
                    }
                }
            }
            yield break;
        }
        /// <summary>基于键相等对两个序列的元素进行关联并对结果进行分组。使用默认的相等比较器对键进行比较。</summary>
        /// <returns>一个包含 <paramref name="TResult" /> 类型的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />，这些元素可通过对两个序列执行分组联接获取。</returns>
        /// <param name="outer">要联接的第一个序列。</param>
        /// <param name="inner">要与第一个序列联接的序列。</param>
        /// <param name="outerKeySelector">用于从第一个序列的每个元素提取联接键的函数。</param>
        /// <param name="innerKeySelector">用于从第二个序列的每个元素提取联接键的函数。</param>
        /// <param name="resultSelector">用于从第一个序列的元素和第二个序列的匹配元素集合中创建结果元素的函数。</param>
        /// <typeparam name="TOuter">第一个序列中的元素的类型。</typeparam>
        /// <typeparam name="TInner">第二个序列中的元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <typeparam name="TResult">结果元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="outer" /> 或 <paramref name="inner" /> 或 <paramref name="outerKeySelector" /> 或 <paramref name="innerKeySelector" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
        {
            if (outer == null)
            {
                throw new ArgumentNullException("outer");
            }
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }
            if (outerKeySelector == null)
            {
                throw new ArgumentNullException("outerKeySelector");
            }
            if (innerKeySelector == null)
            {
                throw new ArgumentNullException("innerKeySelector");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.GroupJoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
        }
        /// <summary>基于键相等对两个序列的元素进行关联并对结果进行分组。使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 对键进行比较。</summary>
        /// <returns>一个包含 <paramref name="TResult" /> 类型的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />，这些元素可通过对两个序列执行分组联接获取。</returns>
        /// <param name="outer">要联接的第一个序列。</param>
        /// <param name="inner">要与第一个序列联接的序列。</param>
        /// <param name="outerKeySelector">用于从第一个序列的每个元素提取联接键的函数。</param>
        /// <param name="innerKeySelector">用于从第二个序列的每个元素提取联接键的函数。</param>
        /// <param name="resultSelector">用于从第一个序列的元素和第二个序列的匹配元素集合中创建结果元素的函数。</param>
        /// <param name="comparer">一个 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />，用于对键进行哈希处理和比较。</param>
        /// <typeparam name="TOuter">第一个序列中的元素的类型。</typeparam>
        /// <typeparam name="TInner">第二个序列中的元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <typeparam name="TResult">结果元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="outer" /> 或 <paramref name="inner" /> 或 <paramref name="outerKeySelector" /> 或 <paramref name="innerKeySelector" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (outer == null)
            {
                throw new ArgumentNullException("outer");
            }
            if (inner == null)
            {
                throw new ArgumentNullException("inner");
            }
            if (outerKeySelector == null)
            {
                throw new ArgumentNullException("outerKeySelector");
            }
            if (innerKeySelector == null)
            {
                throw new ArgumentNullException("innerKeySelector");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.GroupJoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }
        private static IEnumerable<TResult> GroupJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            Lookup<TKey, TInner> lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
            foreach (TOuter current in outer)
            {
                yield return resultSelector(current, lookup[outerKeySelector(current)]);
            }
            yield break;
        }
        /// <summary>根据键按升序对序列的元素排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，其元素按键排序。</returns>
        /// <param name="source">一个要排序的值序列。</param>
        /// <param name="keySelector">用于从元素中提取键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, false);
        }
        /// <summary>使用指定的比较器按升序对序列的元素排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，其元素按键排序。</returns>
        /// <param name="source">一个要排序的值序列。</param>
        /// <param name="keySelector">用于从元素中提取键的函数。</param>
        /// <param name="comparer">一个用于比较键的 <see cref="T:System.Collections.Generic.IComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false);
        }
        /// <summary>根据键按降序对序列的元素排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，将根据键按降序对其元素进行排序。</returns>
        /// <param name="source">一个要排序的值序列。</param>
        /// <param name="keySelector">用于从元素中提取键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, true);
        }
        /// <summary>使用指定的比较器按降序对序列的元素排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，将根据键按降序对其元素进行排序。</returns>
        /// <param name="source">一个要排序的值序列。</param>
        /// <param name="keySelector">用于从元素中提取键的函数。</param>
        /// <param name="comparer">一个用于比较键的 <see cref="T:System.Collections.Generic.IComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true);
        }
        /// <summary>根据某个键按升序对序列中的元素执行后续排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，其元素按键排序。</returns>
        /// <param name="source">一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，包含要排序的元素。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.CreateOrderedEnumerable<TKey>(keySelector, null, false);
        }
        /// <summary>使用指定的比较器按升序对序列中的元素执行后续排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，其元素按键排序。</returns>
        /// <param name="source">一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，包含要排序的元素。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="comparer">一个用于比较键的 <see cref="T:System.Collections.Generic.IComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, false);
        }
        /// <summary>根据某个键按降序对序列中的元素执行后续排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，将根据键按降序对其元素进行排序。</returns>
        /// <param name="source">一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，包含要排序的元素。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.CreateOrderedEnumerable<TKey>(keySelector, null, true);
        }
        /// <summary>使用指定的比较器按降序对序列中的元素执行后续排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，将根据键按降序对其元素进行排序。</returns>
        /// <param name="source">一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，包含要排序的元素。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="comparer">一个用于比较键的 <see cref="T:System.Collections.Generic.IComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, true);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组。</summary>
        /// <returns>在 C# 中为 IEnumerable&lt;IGrouping&lt;TKey, TSource&gt;&gt;，或者在 Visual Basic 中为 IEnumerable(Of IGrouping(Of TKey, TSource))，其中每个 <see cref="T:NewLife.Linq.IGrouping`2" /> 对象都包含一个对象序列和一个键。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, null);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组，并使用指定的比较器对键进行比较。</summary>
        /// <returns>在 C# 中为 IEnumerable&lt;IGrouping&lt;TKey, TSource&gt;&gt;，或者在 Visual Basic 中为 IEnumerable(Of IGrouping(Of TKey, TSource))，其中每个 <see cref="T:NewLife.Linq.IGrouping`2" /> 对象都包含一个对象集合和一个键。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组，并且通过使用指定的函数对每个组中的元素进行投影。</summary>
        /// <returns>在 C# 中为 IEnumerable&lt;IGrouping&lt;TKey, TElement&gt;&gt;，或者在 Visual Basic 中为 IEnumerable(Of IGrouping(Of TKey, TElement))，其中每个 <see cref="T:NewLife.Linq.IGrouping`2" /> 对象都包含一个类型为 <paramref name="TElement" /> 的对象集合和一个键。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="elementSelector">用于将每个源元素映射到 <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">
        ///   <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 或 <paramref name="elementSelector" /> 为 null。</exception>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
        }
        /// <summary>根据键选择器函数对序列中的元素进行分组。通过使用比较器对键进行比较，并且通过使用指定的函数对每个组的元素进行投影。</summary>
        /// <returns>在 C# 中为 IEnumerable&lt;IGrouping&lt;TKey, TElement&gt;&gt;，或者在 Visual Basic 中为 IEnumerable(Of IGrouping(Of TKey, TElement))，其中每个 <see cref="T:NewLife.Linq.IGrouping`2" /> 对象都包含一个类型为 <paramref name="TElement" /> 的对象集合和一个键。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="elementSelector">用于将每个源元素映射到 <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">
        ///   <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 或 <paramref name="elementSelector" /> 为 null。</exception>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值。</summary>
        /// <returns>
        ///   <paramref name="TResult" /> 类型的元素的集合，其中每个元素都表示对一个组及其键的投影。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="resultSelector">用于从每个组中创建结果值的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="resultSelector" /> 返回的结果值的类型。</typeparam>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector, IdentityFunction<TSource>.Instance, resultSelector, null);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值。通过使用指定的函数对每个组的元素进行投影。</summary>
        /// <returns>
        ///   <paramref name="TResult" /> 类型的元素的集合，其中每个元素都表示对一个组及其键的投影。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="elementSelector">用于将每个源元素映射到 <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的函数。</param>
        /// <param name="resultSelector">用于从每个组中创建结果值的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">每个 <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="resultSelector" /> 返回的结果值的类型。</typeparam>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值。通过使用指定的比较器对键进行比较。</summary>
        /// <returns>
        ///   <paramref name="TResult" /> 类型的元素的集合，其中每个元素都表示对一个组及其键的投影。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="resultSelector">用于从每个组中创建结果值的函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="resultSelector" /> 返回的结果值的类型。</typeparam>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector, IdentityFunction<TSource>.Instance, resultSelector, comparer);
        }
        /// <summary>根据指定的键选择器函数对序列中的元素进行分组，并且从每个组及其键中创建结果值。通过使用指定的比较器对键值进行比较，并且通过使用指定的函数对每个组的元素进行投影。</summary>
        /// <returns>
        ///   <paramref name="TResult" /> 类型的元素的集合，其中每个元素都表示对一个组及其键的投影。</returns>
        /// <param name="source">要对其元素进行分组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于提取每个元素的键的函数。</param>
        /// <param name="elementSelector">用于将每个源元素映射到 <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的函数。</param>
        /// <param name="resultSelector">用于从每个组中创建结果值的函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">每个 <see cref="T:NewLife.Linq.IGrouping`2" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="resultSelector" /> 返回的结果值的类型。</typeparam>
        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
        }
        /// <summary>连接两个序列。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含两个输入序列的连接元素。</returns>
        /// <param name="first">要连接的第一个序列。</param>
        /// <param name="second">要与第一个序列连接的序列。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.ConcatIterator<TSource>(first, second);
        }
        private static IEnumerable<TSource> ConcatIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            foreach (TSource current in first)
            {
                yield return current;
            }
            foreach (TSource current2 in second)
            {
                yield return current2;
            }
            yield break;
        }
        /// <summary>通过使用指定的谓词函数合并两个序列。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含两个输入序列的已合并元素。</returns>
        /// <param name="first">要合并的第一个序列。</param>
        /// <param name="second">要合并的第二个序列。</param>
        /// <param name="resultSelector">用于指定如何合并这两个序列的元素的函数。</param>
        /// <typeparam name="TFirst">第一个输入序列中的元素的类型。</typeparam>
        /// <typeparam name="TSecond">第二个输入序列中的元素的类型。</typeparam>
        /// <typeparam name="TResult">结果序列的元素的类型。</typeparam>
        public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            return Enumerable.ZipIterator<TFirst, TSecond, TResult>(first, second, resultSelector);
        }
        private static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            using (IEnumerator<TFirst> enumerator = first.GetEnumerator())
            {
                using (IEnumerator<TSecond> enumerator2 = second.GetEnumerator())
                {
                    while (enumerator.MoveNext() && enumerator2.MoveNext())
                    {
                        yield return resultSelector(enumerator.Current, enumerator2.Current);
                    }
                }
            }
            yield break;
        }
        /// <summary>通过使用默认的相等比较器对值进行比较返回序列中的非重复元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含源序列中的非重复元素。</returns>
        /// <param name="source">要从中移除重复元素的序列。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.DistinctIterator<TSource>(source, null);
        }
        /// <summary>通过使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 对值进行比较返回序列中的非重复元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含源序列中的非重复元素。</returns>
        /// <param name="source">要从中移除重复元素的序列。</param>
        /// <param name="comparer">用于比较值的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.DistinctIterator<TSource>(source, comparer);
        }
        private static IEnumerable<TSource> DistinctIterator<TSource>(IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource current in source)
            {
                if (set.Add(current))
                {
                    yield return current;
                }
            }
            yield break;
        }
        /// <summary>通过使用默认的相等比较器生成两个序列的并集。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含两个输入序列中的元素（重复元素除外）。</returns>
        /// <param name="first">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，它的非重复元素构成联合的第一个集。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，它的非重复元素构成联合的第二个集。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.UnionIterator<TSource>(first, second, null);
        }
        /// <summary>通过使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 生成两个序列的并集。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含两个输入序列中的元素（重复元素除外）。</returns>
        /// <param name="first">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，它的非重复元素构成联合的第一个集。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，它的非重复元素构成联合的第二个集。</param>
        /// <param name="comparer">用于对值进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.UnionIterator<TSource>(first, second, comparer);
        }
        private static IEnumerable<TSource> UnionIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource current in first)
            {
                if (set.Add(current))
                {
                    yield return current;
                }
            }
            foreach (TSource current2 in second)
            {
                if (set.Add(current2))
                {
                    yield return current2;
                }
            }
            yield break;
        }
        /// <summary>通过使用默认的相等比较器对值进行比较生成两个序列的交集。</summary>
        /// <returns>包含组成两个序列交集的元素的序列。</returns>
        /// <param name="first">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将返回其也出现在 <paramref name="second" /> 中的非重复元素。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将返回其也出现在第一个序列中的非重复元素。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.IntersectIterator<TSource>(first, second, null);
        }
        /// <summary>通过使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 对值进行比较以生成两个序列的交集。</summary>
        /// <returns>包含组成两个序列交集的元素的序列。</returns>
        /// <param name="first">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将返回其也出现在 <paramref name="second" /> 中的非重复元素。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将返回其也出现在第一个序列中的非重复元素。</param>
        /// <param name="comparer">用于比较值的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.IntersectIterator<TSource>(first, second, comparer);
        }
        private static IEnumerable<TSource> IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource current in second)
            {
                set.Add(current);
            }
            foreach (TSource current2 in first)
            {
                if (set.Remove(current2))
                {
                    yield return current2;
                }
            }
            yield break;
        }
        /// <summary>通过使用默认的相等比较器对值进行比较生成两个序列的差集。</summary>
        /// <returns>包含两个序列元素的差集的序列。</returns>
        /// <param name="first">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将返回其不在 <paramref name="second" /> 中的元素。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，如果它的元素也出现在第一个序列中，则将导致从返回的序列中移除这些元素。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.ExceptIterator<TSource>(first, second, null);
        }
        /// <summary>通过使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 对值进行比较产生两个序列的差集。</summary>
        /// <returns>包含两个序列元素的差集的序列。</returns>
        /// <param name="first">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将返回其不在 <paramref name="second" /> 中的元素。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，如果它的元素也出现在第一个序列中，则将导致从返回的序列中移除这些元素。</param>
        /// <param name="comparer">用于比较值的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        public static IEnumerable<TSource> Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            return Enumerable.ExceptIterator<TSource>(first, second, comparer);
        }
        private static IEnumerable<TSource> ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            Set<TSource> set = new Set<TSource>(comparer);
            foreach (TSource current in second)
            {
                set.Add(current);
            }
            foreach (TSource current2 in first)
            {
                if (set.Add(current2))
                {
                    yield return current2;
                }
            }
            yield break;
        }
        /// <summary>反转序列中元素的顺序。</summary>
        /// <returns>一个序列，其元素以相反顺序对应于输入序列的元素。</returns>
        /// <param name="source">要反转的值序列。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.ReverseIterator<TSource>(source);
        }
        private static IEnumerable<TSource> ReverseIterator<TSource>(IEnumerable<TSource> source)
        {
            Buffer<TSource> buffer = new Buffer<TSource>(source);
            for (int i = buffer.count - 1; i >= 0; i--)
            {
                yield return buffer.items[i];
            }
            yield break;
        }
        /// <summary>通过使用相应类型的默认相等比较器对序列的元素进行比较，以确定两个序列是否相等。</summary>
        /// <returns>如果根据相应类型的默认相等比较器，两个源序列的长度相等，且其相应元素相等，则为 true；否则为 false。</returns>
        /// <param name="first">一个用于比较 <paramref name="second" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，用于与第一个序列进行比较。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return first.SequenceEqual(second, null);
        }
        /// <summary>使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 对两个序列的元素进行比较，以确定序列是否相等。</summary>
        /// <returns>如果根据 <paramref name="comparer" />，两个源序列的长度相等，且其相应元素相等，则为 true；否则为 false。</returns>
        /// <param name="first">一个用于比较 <paramref name="second" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="second">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，用于与第一个序列进行比较。</param>
        /// <param name="comparer">一个用于比较元素的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">输入序列中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="first" /> 或 <paramref name="second" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" />具有多个元素。</exception>
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TSource>.Default;
            }
            if (first == null)
            {
                throw new ArgumentNullException("first");
            }
            if (second == null)
            {
                throw new ArgumentNullException("second");
            }
            using (IEnumerator<TSource> enumerator = first.GetEnumerator())
            {
                using (IEnumerator<TSource> enumerator2 = second.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (!enumerator2.MoveNext() || !comparer.Equals(enumerator.Current, enumerator2.Current))
                        {
                            bool result = false;
                            return result;
                        }
                    }
                    if (enumerator2.MoveNext())
                    {
                        bool result = false;
                        return result;
                    }
                }
            }
            return true;
        }
        #endregion

        #region 集合转换
        /// <summary>返回类型为 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 的输入。</summary>
        /// <returns>类型为 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 的输入序列。</returns>
        /// <param name="source">类型为 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 的序列。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source)
        {
            return source;
        }
        /// <summary>从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个数组。</summary>
        /// <returns>一个包含输入序列中的元素的数组。</returns>
        /// <param name="source">要从其创建数组的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return new Buffer<TSource>(source).ToArray();
        }
        /// <summary>从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:System.Collections.Generic.List`1" />。</summary>
        /// <returns>一个包含输入序列中元素的 <see cref="T:System.Collections.Generic.List`1" />。</returns>
        /// <param name="source">要从其创建 <see cref="T:System.Collections.Generic.List`1" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return new List<TSource>(source);
        }
        /// <summary>根据指定的键选择器函数，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</summary>
        /// <returns>一个包含键和值的 <see cref="T:System.Collections.Generic.Dictionary`2" />。</returns>
        /// <param name="source">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将从它创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。- 或 -<paramref name="keySelector" /> 产生了一个 null 键。</exception>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="keySelector" /> 为两个元素产生了重复键。</exception>
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.ToDictionary(keySelector, IdentityFunction<TSource>.Instance, null);
        }
        /// <summary>根据指定的键选择器函数和键比较器，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</summary>
        /// <returns>一个包含键和值的 <see cref="T:System.Collections.Generic.Dictionary`2" />。</returns>
        /// <param name="source">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将从它创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。- 或 -<paramref name="keySelector" /> 产生了一个 null 键。</exception>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="keySelector" /> 为两个元素产生了重复键。</exception>
        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return source.ToDictionary(keySelector, IdentityFunction<TSource>.Instance, comparer);
        }
        /// <summary>根据指定的键选择器和元素选择器函数，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.Dictionary`2" />，包含从输入序列中选择的类型为 <paramref name="TElement" /> 的值。</returns>
        /// <param name="source">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将从它创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="elementSelector">用于从每个元素产生结果元素值的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">
        ///   <paramref name="elementSelector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 或 <paramref name="elementSelector" /> 为 null。- 或 -<paramref name="keySelector" /> 产生了一个 null 键。</exception>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="keySelector" /> 为两个元素产生了重复键。</exception>
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return source.ToDictionary(keySelector, elementSelector, null);
        }
        /// <summary>根据指定的键选择器函数、比较器和元素选择器函数从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.Dictionary`2" />，包含从输入序列中选择的类型为 <paramref name="TElement" /> 的值。</returns>
        /// <param name="source">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，将从它创建一个 <see cref="T:System.Collections.Generic.Dictionary`2" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="elementSelector">用于从每个元素产生结果元素值的转换函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">
        ///   <paramref name="elementSelector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 或 <paramref name="elementSelector" /> 为 null。- 或 -<paramref name="keySelector" /> 产生了一个 null 键。</exception>
        /// <exception cref="T:System.ArgumentException">
        ///   <paramref name="keySelector" /> 为两个元素产生了重复键。</exception>
        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
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
            Dictionary<TKey, TElement> dictionary = new Dictionary<TKey, TElement>(comparer);
            foreach (TSource current in source)
            {
                dictionary.Add(keySelector(current), elementSelector(current));
            }
            return dictionary;
        }
        /// <summary>根据指定的键选择器函数，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:NewLife.Linq.Lookup`2" />。</summary>
        /// <returns>一个包含键和值的 <see cref="T:NewLife.Linq.Lookup`2" />。</returns>
        /// <param name="source">要从其创建 <see cref="T:NewLife.Linq.Lookup`2" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return Lookup<TKey, TSource>.Create<TSource>(source, keySelector, IdentityFunction<TSource>.Instance, null);
        }
        /// <summary>根据指定的键选择器函数和键比较器，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:NewLife.Linq.Lookup`2" />。</summary>
        /// <returns>一个包含键和值的 <see cref="T:NewLife.Linq.Lookup`2" />。</returns>
        /// <param name="source">要从其创建 <see cref="T:NewLife.Linq.Lookup`2" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 为 null。</exception>
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return Lookup<TKey, TSource>.Create<TSource>(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
        }
        /// <summary>根据指定的键选择器和元素选择器函数，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:NewLife.Linq.Lookup`2" />。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.Lookup`2" />，包含从输入序列中选择的类型为 <paramref name="TElement" /> 的值。</returns>
        /// <param name="source">要从其创建 <see cref="T:NewLife.Linq.Lookup`2" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="elementSelector">用于从每个元素产生结果元素值的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">
        ///   <paramref name="elementSelector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 或 <paramref name="elementSelector" /> 为 null。</exception>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return Lookup<TKey, TElement>.Create<TSource>(source, keySelector, elementSelector, null);
        }
        /// <summary>根据指定的键选择器函数、比较器和元素选择器函数，从 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 创建一个 <see cref="T:NewLife.Linq.Lookup`2" />。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.Lookup`2" />，包含从输入序列中选择的类型为 <paramref name="TElement" /> 的值。</returns>
        /// <param name="source">要从其创建 <see cref="T:NewLife.Linq.Lookup`2" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="keySelector">用于从每个元素中提取键的函数。</param>
        /// <param name="elementSelector">用于从每个元素产生结果元素值的转换函数。</param>
        /// <param name="comparer">一个用于对键进行比较的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 返回的键的类型。</typeparam>
        /// <typeparam name="TElement">
        ///   <paramref name="elementSelector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="keySelector" /> 或 <paramref name="elementSelector" /> 为 null。</exception>
        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            return Lookup<TKey, TElement>.Create<TSource>(source, keySelector, elementSelector, comparer);
        }
        #endregion

        #region 集合元素转换
        /// <summary>返回指定序列的元素；如果序列为空，则返回单一实例集合中的类型参数的默认值。</summary>
        /// <returns>如果 <paramref name="source" /> 为空，则为包含 <paramref name="TSource" /> 类型的默认值的 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 对象；否则为 <paramref name="source" />。</returns>
        /// <param name="source">序列为空时返回默认值的序列。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
        {
            return source.DefaultIfEmpty(default(TSource));
        }
        /// <summary>返回指定序列中的元素；如果序列为空，则返回单一实例集合中的指定值。</summary>
        /// <returns>在 <paramref name="source" /> 为空的情况下包含 <paramref name="defaultValue" /> 的 <see cref="T:System.Collections.Generic.IEnumerable`1" />；否则为 <paramref name="source" />。</returns>
        /// <param name="source">序列为空时为其返回指定值的序列。</param>
        /// <param name="defaultValue">序列为空时要返回的值。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.DefaultIfEmptyIterator<TSource>(source, defaultValue);
        }
        private static IEnumerable<TSource> DefaultIfEmptyIterator<TSource>(IEnumerable<TSource> source, TSource defaultValue)
        {
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    do
                    {
                        yield return enumerator.Current;
                    }
                    while (enumerator.MoveNext());
                }
                else
                {
                    yield return defaultValue;
                }
            }
            yield break;
        }
        /// <summary>根据指定类型筛选 <see cref="T:System.Collections.IEnumerable" /> 的元素。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含类型为 <paramref name="TResult" /> 的输入序列中的元素。</returns>
        /// <param name="source">
        ///   <see cref="T:System.Collections.IEnumerable" />，其元素用于筛选。</param>
        /// <typeparam name="TResult">筛选序列元素所根据的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.OfTypeIterator<TResult>(source);
        }
        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object current in source)
            {
                if (current is TResult)
                {
                    yield return (TResult)current;
                }
            }
            yield break;
        }
        /// <summary>将 <see cref="T:System.Collections.IEnumerable" /> 的元素转换为指定的类型。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含已转换为指定类型的源序列的每个元素。</returns>
        /// <param name="source">包含要转换的元素的 <see cref="T:System.Collections.IEnumerable" />。</param>
        /// <typeparam name="TResult">
        ///   <paramref name="source" /> 中的元素要转换成的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidCastException">序列中的元素不能强制转换为 <paramref name="TResult" /> 类型。</exception>
        public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
        {
            IEnumerable<TResult> enumerable = source as IEnumerable<TResult>;
            if (enumerable != null)
            {
                return enumerable;
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return Enumerable.CastIterator<TResult>(source);
        }
        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object current in source)
            {
                yield return (TResult)current;
            }
            yield break;
        }
        #endregion

        #region 元素定位
        /// <summary>返回序列中的第一个元素。</summary>
        /// <returns>返回指定序列中的第一个元素。</returns>
        /// <param name="source">要返回其第一个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">源序列为空。</exception>
        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        return enumerator.Current;
                    }
                }
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回序列中满足指定条件的第一个元素。</summary>
        /// <returns>序列中通过指定谓词函数中的测试的第一个元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">没有元素满足 <paramref name="predicate" /> 中的条件。- 或 -源序列为空。</exception>
        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    return current;
                }
            }
            throw new InvalidOperationException("NoMatch"); ;
        }
        /// <summary>返回序列中的第一个元素；如果序列中不包含任何元素，则返回默认值。</summary>
        /// <returns>如果 <paramref name="source" /> 为空，则返回 default(<paramref name="TSource" />)；否则返回 <paramref name="source" /> 中的第一个元素。</returns>
        /// <param name="source">要返回其第一个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        return enumerator.Current;
                    }
                }
            }
            return default(TSource);
        }
        /// <summary>返回序列中满足条件的第一个元素；如果未找到这样的元素，则返回默认值。</summary>
        /// <returns>如果 <paramref name="source" /> 为空或没有元素通过 <paramref name="predicate" /> 指定的测试，则返回 default(<paramref name="TSource" />)，否则返回 <paramref name="source" /> 中通过 <paramref name="predicate" /> 指定的测试的第一个元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    return current;
                }
            }
            return default(TSource);
        }
        /// <summary>返回序列的最后一个元素。</summary>
        /// <returns>源序列中最后位置处的值。</returns>
        /// <param name="source">要返回其最后一个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">源序列为空。</exception>
        public static TSource Last<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                int count = list.Count;
                if (count > 0)
                {
                    return list[count - 1];
                }
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        TSource current;
                        do
                        {
                            current = enumerator.Current;
                        }
                        while (enumerator.MoveNext());
                        return current;
                    }
                }
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回序列中满足指定条件的最后一个元素。</summary>
        /// <returns>序列中通过指定谓词函数中的测试的最后一个元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">没有元素满足 <paramref name="predicate" /> 中的条件。- 或 -源序列为空。</exception>
        public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            TSource result = default(TSource);
            bool flag = false;
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    result = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return result;
            }
            throw new InvalidOperationException("NoMatch"); ;
        }
        /// <summary>返回序列中的最后一个元素；如果序列中不包含任何元素，则返回默认值。</summary>
        /// <returns>如果源序列为空，则返回 default(<paramref name="TSource" />)；否则返回 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 中的最后一个元素。</returns>
        /// <param name="source">要返回其最后一个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                int count = list.Count;
                if (count > 0)
                {
                    return list[count - 1];
                }
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        TSource current;
                        do
                        {
                            current = enumerator.Current;
                        }
                        while (enumerator.MoveNext());
                        return current;
                    }
                }
            }
            return default(TSource);
        }
        /// <summary>返回序列中满足条件的最后一个元素；如果未找到这样的元素，则返回默认值。</summary>
        /// <returns>如果序列为空或没有元素通过谓词函数中的测试，则返回 default(<paramref name="TSource" />)；否则返回通过谓词函数中的测试的最后一个元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            TSource result = default(TSource);
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    result = current;
                }
            }
            return result;
        }
        /// <summary>返回序列的唯一元素；如果该序列并非恰好包含一个元素，则会引发异常。</summary>
        /// <returns>输入序列的单个元素。</returns>
        /// <param name="source">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，用于返回单个元素。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">输入序列包含多个元素。- 或 -输入序列为空。</exception>
        public static TSource Single<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                switch (list.Count)
                {
                    case 0:
                        {
                            throw new InvalidOperationException("NoElements"); ;
                        }
                    case 1:
                        {
                            return list[0];
                        }
                }
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                    {
                        throw new InvalidOperationException("NoElements"); ;
                    }
                    TSource current = enumerator.Current;
                    if (!enumerator.MoveNext())
                    {
                        return current;
                    }
                }
            }
            throw new InvalidOperationException("MoreThanOneElement"); ;
        }
        /// <summary>返回序列中满足指定条件的唯一元素；如果有多个这样的元素存在，则会引发异常。</summary>
        /// <returns>输入序列中满足条件的单个元素。</returns>
        /// <param name="source">要从中返回单个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">没有元素满足 <paramref name="predicate" /> 中的条件。- 或 -多个元素满足 <paramref name="predicate" /> 中的条件。- 或 -源序列为空。</exception>
        public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            //checked
            //{
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            TSource result = default(TSource);
            long num = 0L;
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    result = current;
                    num += 1L;
                }
            }
            long num2 = num;
            //}
            if (num2 <= 1L && num2 >= 0L)
            {
                switch ((int)num2)
                {
                    case 0:
                        throw new InvalidOperationException("NoMatch"); ;
                    case 1:
                        return result;
                }
            }
            throw new InvalidOperationException("MoreThanOneMatch"); ;
        }
        /// <summary>返回序列中的唯一元素；如果该序列为空，则返回默认值；如果该序列包含多个元素，此方法将引发异常。</summary>
        /// <returns>返回输入序列的单个元素；如果序列不包含任何元素，则返回 default(<paramref name="TSource" />)。</returns>
        /// <param name="source">要返回其单个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">输入序列包含多个元素。</exception>
        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                switch (list.Count)
                {
                    case 0:
                        {
                            return default(TSource);
                        }
                    case 1:
                        {
                            return list[0];
                        }
                }
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    if (!enumerator.MoveNext())
                    {
                        TSource result = default(TSource);
                        return result;
                    }
                    TSource current = enumerator.Current;
                    if (!enumerator.MoveNext())
                    {
                        TSource result = current;
                        return result;
                    }
                }
            }
            throw new InvalidOperationException("MoreThanOneElement"); ;
        }
        /// <summary>返回序列中满足指定条件的唯一元素；如果这类元素不存在，则返回默认值；如果有多个元素满足该条件，此方法将引发异常。</summary>
        /// <returns>如果未找到这样的元素，则返回输入序列中满足条件的单个元素或 default (<paramref name="TSource" />)。</returns>
        /// <param name="source">要从中返回单个元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">多个元素满足 <paramref name="predicate" /> 中的条件。</exception>
        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            //checked
            //{
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            TSource result = default(TSource);
            long num = 0L;
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    result = current;
                    num += 1L;
                }
            }
            long num2 = num;
            //}
            if (num2 <= 1L && num2 >= 0L)
            {
                switch ((int)num2)
                {
                    case 0:
                        {
                            return default(TSource);
                        }
                    case 1:
                        {
                            return result;
                        }
                }
            }
            throw new InvalidOperationException("MoreThanOneMatch"); ;
        }
        /// <summary>返回序列中指定索引处的元素。</summary>
        /// <returns>源序列中指定位置处的元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="index">要检索的从零开始的元素索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="index" /> 小于零或大于等于 <paramref name="source" /> 中的元素数量。</exception>
        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null) throw new ArgumentNullException("source");

            IList<TSource> list = source as IList<TSource>;
            if (list != null) return list[index];

            if (index < 0) throw new ArgumentOutOfRangeException("index");

            //TSource current;
            //using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            //{
            //    while (enumerator.MoveNext())
            //    {
            //        if (index == 0)
            //        {
            //            current = enumerator.Current;
            //            return current;
            //        }
            //        index--;
            //    }
            //    throw new ArgumentOutOfRangeException("index");
            //}
            //return current;

            foreach (TSource item in source)
            {
                if (index-- == 0) return item;
            }
            throw new ArgumentOutOfRangeException("index");
        }
        /// <summary>返回序列中指定索引处的元素；如果索引超出范围，则返回默认值。</summary>
        /// <returns>如果索引超出源序列的范围，则为 default(<paramref name="TSource" />)；否则为源序列中指定位置处的元素。</returns>
        /// <param name="source">要从中返回元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="index">要检索的从零开始的元素索引。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (index >= 0)
            {
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    if (index < list.Count)
                    {
                        return list[index];
                    }
                }
                else
                {
                    //using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                    //{
                    //    while (enumerator.MoveNext())
                    //    {
                    //        if (index == 0)
                    //        {
                    //            return enumerator.Current;
                    //        }
                    //        index--;
                    //    }
                    //}

                    foreach (TSource item in source)
                    {
                        if (index-- == 0) return item;
                    }
                }
            }
            return default(TSource);
        }
        #endregion

        #region 产生集合
        /// <summary>生成指定范围内的整数的序列。</summary>
        /// <returns>C# 中的 IEnumerable&lt;Int32&gt; 或 Visual Basic 中包含某个范围内的顺序整数的 IEnumerable(Of Int32)。</returns>
        /// <param name="start">序列中第一个整数的值。</param>
        /// <param name="count">要生成的顺序整数的数目。</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="count" /> 小于 0。- 或 -<paramref name="start" /> + <paramref name="count" /> -1 大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static IEnumerable<int> Range(int start, int count)
        {
            long num = (long)start + (long)count - 1L;
            if (count < 0 || num > 2147483647L)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return Enumerable.RangeIterator(start, count);
        }
        private static IEnumerable<int> RangeIterator(int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return start + i;
            }
            yield break;
        }
        /// <summary>生成包含一个重复值的序列。</summary>
        /// <returns>一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，包含一个重复值。</returns>
        /// <param name="element">要重复的值。</param>
        /// <param name="count">在生成序列中重复该值的次数。</param>
        /// <typeparam name="TResult">要在结果序列中重复的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///   <paramref name="count" /> 小于 0。</exception>
        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return Enumerable.RepeatIterator<TResult>(element, count);
        }
        private static IEnumerable<TResult> RepeatIterator<TResult>(TResult element, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return element;
            }
            yield break;
        }
        /// <summary>返回一个具有指定的类型参数的空 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</summary>
        /// <returns>一个类型参数为 <paramref name="TResult" /> 的空 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</returns>
        /// <typeparam name="TResult">分配给返回的泛型 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 的类型参数的类型。</typeparam>
        public static IEnumerable<TResult> Empty<TResult>()
        {
            return EmptyEnumerable<TResult>.Instance;
        }
        #endregion

        #region 集合统计
        /// <summary>确定序列是否包含任何元素。</summary>
        /// <returns>如果源序列包含任何元素，则为 true；否则为 false。</returns>
        /// <param name="source">要检查是否为空的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static bool Any<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>确定序列中的任何元素是否都满足条件。</summary>
        /// <returns>如果源序列中的任何元素都通过指定谓词中的测试，则为 true；否则为 false。</returns>
        /// <param name="source">一个 <see cref="T:System.Collections.Generic.IEnumerable`1" />，其元素将应用谓词。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (TSource current in source)
            {
                if (predicate(current))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>确定序列中的所有元素是否满足条件。</summary>
        /// <returns>如果源序列中的每个元素都通过指定谓词中的测试，或者序列为空，则为 true；否则为 false。</returns>
        /// <param name="source">包含要应用谓词的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (TSource current in source)
            {
                if (!predicate(current))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>返回序列中的元素数量。</summary>
        /// <returns>输入序列中的元素数量。</returns>
        /// <param name="source">包含要计数的元素的序列。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">
        ///   <paramref name="source" /> 中的元素数量大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static int Count<TSource>(this IEnumerable<TSource> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                ICollection<TSource> collection = source as ICollection<TSource>;
                if (collection != null)
                {
                    return collection.Count;
                }
                ICollection collection2 = source as ICollection;
                if (collection2 != null)
                {
                    return collection2.Count;
                }
                int num = 0;
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        num++;
                    }
                }
                return num;
            }
        }
        /// <summary>返回一个数字，表示在指定的序列中满足条件的元素数量。</summary>
        /// <returns>一个数字，表示序列中满足谓词函数条件的元素数量。</returns>
        /// <param name="source">包含要测试和计数的元素的序列。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">
        ///   <paramref name="source" /> 中的元素数量大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                if (predicate == null)
                {
                    throw new ArgumentNullException("predicate");
                }
                int num = 0;
                foreach (TSource current in source)
                {
                    if (predicate(current))
                    {
                        num++;
                    }
                }
                return num;
            }
        }
        /// <summary>返回一个 <see cref="T:System.Int64" />，表示序列中的元素的总数量。</summary>
        /// <returns>源序列中的元素的数量。</returns>
        /// <param name="source">包含要进行计数的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">元素的数量超过 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static long LongCount<TSource>(this IEnumerable<TSource> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        num += 1L;
                    }
                }
                return num;
            }
        }
        /// <summary>返回一个 <see cref="T:System.Int64" />，表示序列中满足条件的元素的数量。</summary>
        /// <returns>一个数字，表示序列中满足谓词函数条件的元素数量。</returns>
        /// <param name="source">包含要进行计数的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">匹配元素的数量超过 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static long LongCount<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                if (predicate == null)
                {
                    throw new ArgumentNullException("predicate");
                }
                long num = 0L;
                foreach (TSource current in source)
                {
                    if (predicate(current))
                    {
                        num += 1L;
                    }
                }
                return num;
            }
        }
        /// <summary>通过使用默认的相等比较器确定序列是否包含指定的元素。</summary>
        /// <returns>如果源序列包含具有指定值的元素，则为 true；否则为 false。</returns>
        /// <param name="source">要在其中定位某个值的序列。</param>
        /// <param name="value">要在序列中定位的值。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
        {
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null)
            {
                return collection.Contains(value);
            }
            return source.Contains(value, null);
        }
        /// <summary>通过使用指定的 <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> 确定序列是否包含指定的元素。</summary>
        /// <returns>如果源序列包含具有指定值的元素，则为 true；否则为 false。</returns>
        /// <param name="source">要在其中定位某个值的序列。</param>
        /// <param name="value">要在序列中定位的值。</param>
        /// <param name="comparer">一个对值进行比较的相等比较器。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TSource>.Default;
            }
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            foreach (TSource current in source)
            {
                if (comparer.Equals(current, value))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 累加、求和、最大、最小、平均
        /// <summary>对序列应用累加器函数。</summary>
        /// <returns>累加器的最终值。</returns>
        /// <param name="source">要聚合的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="func">要对每个元素调用的累加器函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="func" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }
            TSource result;
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("NoElements"); ;
                }
                TSource tSource = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    tSource = func(tSource, enumerator.Current);
                }
                result = tSource;
            }
            return result;
        }
        /// <summary>对序列应用累加器函数。将指定的种子值用作累加器初始值。</summary>
        /// <returns>累加器的最终值。</returns>
        /// <param name="source">要聚合的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="seed">累加器的初始值。</param>
        /// <param name="func">要对每个元素调用的累加器函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TAccumulate">累加器值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="func" /> 为 null。</exception>
        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }
            TAccumulate tAccumulate = seed;
            foreach (TSource current in source)
            {
                tAccumulate = func(tAccumulate, current);
            }
            return tAccumulate;
        }
        /// <summary>对序列应用累加器函数。将指定的种子值用作累加器的初始值，并使用指定的函数选择结果值。</summary>
        /// <returns>已转换的累加器最终值。</returns>
        /// <param name="source">要聚合的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="seed">累加器的初始值。</param>
        /// <param name="func">要对每个元素调用的累加器函数。</param>
        /// <param name="resultSelector">将累加器的最终值转换为结果值的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TAccumulate">累加器值的类型。</typeparam>
        /// <typeparam name="TResult">结果值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="func" /> 或 <paramref name="resultSelector" /> 为 null。</exception>
        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (func == null)
            {
                throw new ArgumentNullException("func");
            }
            if (resultSelector == null)
            {
                throw new ArgumentNullException("resultSelector");
            }
            TAccumulate tAccumulate = seed;
            foreach (TSource current in source)
            {
                tAccumulate = func(tAccumulate, current);
            }
            return resultSelector(tAccumulate);
        }
        /// <summary>计算 <see cref="T:System.Int32" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">一个要计算和的 <see cref="T:System.Int32" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static int Sum(this IEnumerable<int> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                int num = 0;
                foreach (int current in source)
                {
                    num += current;
                }
                return num;
            }
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int32" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">要计算和的可以为 null 的 <see cref="T:System.Int32" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static int? Sum(this IEnumerable<int?> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                int num = 0;
                foreach (int? current in source)
                {
                    if (current.HasValue)
                    {
                        num += current.GetValueOrDefault();
                    }
                }
                return new int?(num);
            }
        }
        /// <summary>计算 <see cref="T:System.Int64" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">一个要计算和的 <see cref="T:System.Int64" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static long Sum(this IEnumerable<long> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                foreach (long current in source)
                {
                    num += current;
                }
                return num;
            }
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int64" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">要计算和的可以为 null 的 <see cref="T:System.Int64" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static long? Sum(this IEnumerable<long?> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                foreach (long? current in source)
                {
                    if (current.HasValue)
                    {
                        num += current.GetValueOrDefault();
                    }
                }
                return new long?(num);
            }
        }
        /// <summary>计算 <see cref="T:System.Single" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">一个要计算和的 <see cref="T:System.Single" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static float Sum(this IEnumerable<float> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            foreach (float num2 in source)
            {
                num += (double)num2;
            }
            return (float)num;
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Single" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">要计算和的可以为 null 的 <see cref="T:System.Single" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static float? Sum(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            foreach (float? current in source)
            {
                if (current.HasValue)
                {
                    num += (double)current.GetValueOrDefault();
                }
            }
            return new float?((float)num);
        }
        /// <summary>计算 <see cref="T:System.Double" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">一个要计算和的 <see cref="T:System.Double" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static double Sum(this IEnumerable<double> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            foreach (double num2 in source)
            {
                num += num2;
            }
            return num;
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Double" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">要计算和的可以为 null 的 <see cref="T:System.Double" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static double? Sum(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            foreach (double? current in source)
            {
                if (current.HasValue)
                {
                    num += current.GetValueOrDefault();
                }
            }
            return new double?(num);
        }
        /// <summary>计算 <see cref="T:System.Decimal" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">一个要计算和的 <see cref="T:System.Decimal" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal Sum(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            decimal num = 0m;
            foreach (decimal current in source)
            {
                num += current;
            }
            return num;
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Decimal" /> 值序列之和。</summary>
        /// <returns>序列值之和。</returns>
        /// <param name="source">要计算和的可以为 null 的 <see cref="T:System.Decimal" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal? Sum(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            decimal num = 0m;
            foreach (decimal? current in source)
            {
                if (current.HasValue)
                {
                    num += current.GetValueOrDefault();
                }
            }
            return new decimal?(num);
        }
        /// <summary>计算 <see cref="T:System.Int32" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int32" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int32.MaxValue" />。</exception>
        public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算 <see cref="T:System.Int64" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int64" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算 <see cref="T:System.Single" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Single" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算 <see cref="T:System.Double" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Double" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算 <see cref="T:System.Decimal" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Decimal" /> 值序列的和，这些值是通过对输入序列中的每个元素调用转换函数得来的。</summary>
        /// <returns>投影值之和。</returns>
        /// <param name="source">用于计算和的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Sum();
        }
        /// <summary>返回 <see cref="T:System.Int32" /> 值序列中的最小值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个 <see cref="T:System.Int32" /> 值序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static int Min(this IEnumerable<int> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int num = 0;
            bool flag = false;
            foreach (int current in source)
            {
                if (flag)
                {
                    if (current < num)
                    {
                        num = current;
                    }
                }
                else
                {
                    num = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回 <see cref="T:System.Int32" /> 值（可空）序列中的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Int32&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Int32)。</returns>
        /// <param name="source">一个可空 <see cref="T:System.Int32" /> 值的序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static int? Min(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int? num = null;
            foreach (int? current in source)
            {
                if (num.HasValue)
                {
                    int? num2 = current;
                    int? num3 = num;
                    if (num2.GetValueOrDefault() >= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue))
                    {
                        continue;
                    }
                }
                num = current;
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Int64" /> 值序列中的最小值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个 <see cref="T:System.Int64" /> 值序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static long Min(this IEnumerable<long> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            long num = 0L;
            bool flag = false;
            foreach (long current in source)
            {
                if (flag)
                {
                    if (current < num)
                    {
                        num = current;
                    }
                }
                else
                {
                    num = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回 <see cref="T:System.Int64" /> 值（可空）序列中的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Int64&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Int64)。</returns>
        /// <param name="source">一个可空 <see cref="T:System.Int64" /> 值的序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static long? Min(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            long? num = null;
            foreach (long? current in source)
            {
                if (num.HasValue)
                {
                    long? num2 = current;
                    long? num3 = num;
                    if (num2.GetValueOrDefault() >= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue))
                    {
                        continue;
                    }
                }
                num = current;
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Single" /> 值序列中的最小值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个 <see cref="T:System.Single" /> 值序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static float Min(this IEnumerable<float> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            float num = 0f;
            bool flag = false;
            foreach (float num2 in source)
            {
                if (flag)
                {
                    if (num2 < num || float.IsNaN(num2))
                    {
                        num = num2;
                    }
                }
                else
                {
                    num = num2;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回 <see cref="T:System.Single" /> 值（可空）序列中的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Single&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Single)。</returns>
        /// <param name="source">一个可空 <see cref="T:System.Single" /> 值的序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static float? Min(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            float? num = null;
            foreach (float? current in source)
            {
                if (current.HasValue)
                {
                    if (num.HasValue)
                    {
                        float? num2 = current;
                        float? num3 = num;
                        if ((num2.GetValueOrDefault() >= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue)) && !float.IsNaN(current.Value))
                        {
                            continue;
                        }
                    }
                    num = current;
                }
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Double" /> 值序列中的最小值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个 <see cref="T:System.Double" /> 值序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static double Min(this IEnumerable<double> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            bool flag = false;
            foreach (double num2 in source)
            {
                if (flag)
                {
                    if (num2 < num || double.IsNaN(num2))
                    {
                        num = num2;
                    }
                }
                else
                {
                    num = num2;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回 <see cref="T:System.Double" /> 值（可空）序列中的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Double&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Double)。</returns>
        /// <param name="source">一个可空 <see cref="T:System.Double" /> 值的序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static double? Min(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double? num = null;
            foreach (double? current in source)
            {
                if (current.HasValue)
                {
                    if (num.HasValue)
                    {
                        double? num2 = current;
                        double? num3 = num;
                        if ((num2.GetValueOrDefault() >= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue)) && !double.IsNaN(current.Value))
                        {
                            continue;
                        }
                    }
                    num = current;
                }
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Decimal" /> 值序列中的最小值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个 <see cref="T:System.Decimal" /> 值序列，用于确定最大值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static decimal Min(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            decimal num = 0m;
            bool flag = false;
            foreach (decimal current in source)
            {
                if (flag)
                {
                    if (current < num)
                    {
                        num = current;
                    }
                }
                else
                {
                    num = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回 <see cref="T:System.Decimal" /> 值（可空）序列中的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Decimal&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Decimal)。</returns>
        /// <param name="source">一个可空 <see cref="T:System.Decimal" /> 值的序列，用于确定最小值。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static decimal? Min(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            decimal? num = null;
            foreach (decimal? current in source)
            {
                if (num.HasValue)
                {
                    decimal? num2 = current;
                    decimal? num3 = num;
                    if (!(num2.GetValueOrDefault() < num3.GetValueOrDefault()) || !(num2.HasValue & num3.HasValue))
                    {
                        continue;
                    }
                }
                num = current;
            }
            return num;
        }
        /// <summary>返回泛型序列中的最小值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static TSource Min<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            Comparer<TSource> @default = Comparer<TSource>.Default;
            TSource tSource = default(TSource);
            if (tSource == null)
            {
                foreach (TSource current in source)
                {
                    if (current != null && (tSource == null || @default.Compare(current, tSource) < 0))
                    {
                        tSource = current;
                    }
                }
                return tSource;
            }
            bool flag = false;
            foreach (TSource current2 in source)
            {
                if (flag)
                {
                    if (@default.Compare(current2, tSource) < 0)
                    {
                        tSource = current2;
                    }
                }
                else
                {
                    tSource = current2;
                    flag = true;
                }
            }
            if (flag)
            {
                return tSource;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最小 <see cref="T:System.Int32" /> 值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Int32" /> 的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Int32&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Int32)。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最小 <see cref="T:System.Int64" /> 值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Int64" /> 的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Int64&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Int64)。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最小 <see cref="T:System.Single" /> 值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Single" /> 的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Single&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Single)。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static float? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最小 <see cref="T:System.Double" /> 值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Double" /> 的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Double&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Double)。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static double? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最小 <see cref="T:System.Decimal" /> 值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static decimal Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Decimal" /> 的最小值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Decimal&gt; 的值或 Visual Basic 中与序列中最小值对应的 Nullable(Of Decimal)。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static decimal? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Min();
        }
        /// <summary>调用泛型序列的每个元素上的转换函数并返回最小结果值。</summary>
        /// <returns>序列中的最小值。</returns>
        /// <param name="source">一个值序列，用于确定最小值。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="selector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.Select(selector).Min<TResult>();
        }
        /// <summary>返回 <see cref="T:System.Int32" /> 值序列中的最大值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的 <see cref="T:System.Int32" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static int Max(this IEnumerable<int> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int num = 0;
            bool flag = false;
            foreach (int current in source)
            {
                if (flag)
                {
                    if (current > num)
                    {
                        num = current;
                    }
                }
                else
                {
                    num = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回可以为 null 的 <see cref="T:System.Int32" /> 值序列中的最大值。</summary>
        /// <returns>一个与序列中的最大值对应的值，该值的类型在 C# 中为 Nullable&lt;Int32&gt;，在 Visual Basic 中为 Nullable(Of Int32)。</returns>
        /// <param name="source">要确定其最大值的可以为 null 的 <see cref="T:System.Int32" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static int? Max(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            int? num = null;
            foreach (int? current in source)
            {
                if (num.HasValue)
                {
                    int? num2 = current;
                    int? num3 = num;
                    if (num2.GetValueOrDefault() <= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue))
                    {
                        continue;
                    }
                }
                num = current;
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Int64" /> 值序列中的最大值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的 <see cref="T:System.Int64" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static long Max(this IEnumerable<long> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            long num = 0L;
            bool flag = false;
            foreach (long current in source)
            {
                if (flag)
                {
                    if (current > num)
                    {
                        num = current;
                    }
                }
                else
                {
                    num = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回可以为 null 的 <see cref="T:System.Int64" /> 值序列中的最大值。</summary>
        /// <returns>一个与序列中的最大值对应的值，该值的类型在 C# 中为 Nullable&lt;Int64&gt;，在 Visual Basic 中为 Nullable(Of Int64)。</returns>
        /// <param name="source">要确定其最大值的可以为 null 的 <see cref="T:System.Int64" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static long? Max(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            long? num = null;
            foreach (long? current in source)
            {
                if (num.HasValue)
                {
                    long? num2 = current;
                    long? num3 = num;
                    if (num2.GetValueOrDefault() <= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue))
                    {
                        continue;
                    }
                }
                num = current;
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Double" /> 值序列中的最大值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的 <see cref="T:System.Double" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static double Max(this IEnumerable<double> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            bool flag = false;
            foreach (double num2 in source)
            {
                if (flag)
                {
                    if (num2 > num || double.IsNaN(num))
                    {
                        num = num2;
                    }
                }
                else
                {
                    num = num2;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回可以为 null 的 <see cref="T:System.Double" /> 值序列中的最大值。</summary>
        /// <returns>一个与序列中的最大值对应的值，该值的类型在 C# 中为 Nullable&lt;Double&gt;，在 Visual Basic 中为 Nullable(Of Double)。</returns>
        /// <param name="source">要确定其最大值的可以为 null 的 <see cref="T:System.Double" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static double? Max(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double? num = null;
            foreach (double? current in source)
            {
                if (current.HasValue)
                {
                    if (num.HasValue)
                    {
                        double? num2 = current;
                        double? num3 = num;
                        if ((num2.GetValueOrDefault() <= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue)) && !double.IsNaN(num.Value))
                        {
                            continue;
                        }
                    }
                    num = current;
                }
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Single" /> 值序列中的最大值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的 <see cref="T:System.Single" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static float Max(this IEnumerable<float> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            float num = 0f;
            bool flag = false;
            foreach (float num2 in source)
            {
                if (flag)
                {
                    if (num2 > num || double.IsNaN((double)num))
                    {
                        num = num2;
                    }
                }
                else
                {
                    num = num2;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回可以为 null 的 <see cref="T:System.Single" /> 值序列中的最大值。</summary>
        /// <returns>一个与序列中的最大值对应的值，该值的类型在 C# 中为 Nullable&lt;Single&gt;，在 Visual Basic 中为 Nullable(Of Single)。</returns>
        /// <param name="source">要确定其最大值的可以为 null 的 <see cref="T:System.Single" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static float? Max(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            float? num = null;
            foreach (float? current in source)
            {
                if (current.HasValue)
                {
                    if (num.HasValue)
                    {
                        float? num2 = current;
                        float? num3 = num;
                        if ((num2.GetValueOrDefault() <= num3.GetValueOrDefault() || !(num2.HasValue & num3.HasValue)) && !float.IsNaN(num.Value))
                        {
                            continue;
                        }
                    }
                    num = current;
                }
            }
            return num;
        }
        /// <summary>返回 <see cref="T:System.Decimal" /> 值序列中的最大值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的 <see cref="T:System.Decimal" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static decimal Max(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            decimal num = 0m;
            bool flag = false;
            foreach (decimal current in source)
            {
                if (flag)
                {
                    if (current > num)
                    {
                        num = current;
                    }
                }
                else
                {
                    num = current;
                    flag = true;
                }
            }
            if (flag)
            {
                return num;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>返回可以为 null 的 <see cref="T:System.Decimal" /> 值序列中的最大值。</summary>
        /// <returns>一个与序列中的最大值对应的值，该值的类型在 C# 中为 Nullable&lt;Decimal&gt;，在 Visual Basic 中为 Nullable(Of Decimal)。</returns>
        /// <param name="source">要确定其最大值的可以为 null 的 <see cref="T:System.Decimal" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static decimal? Max(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            decimal? num = null;
            foreach (decimal? current in source)
            {
                if (num.HasValue)
                {
                    decimal? num2 = current;
                    decimal? num3 = num;
                    if (!(num2.GetValueOrDefault() > num3.GetValueOrDefault()) || !(num2.HasValue & num3.HasValue))
                    {
                        continue;
                    }
                }
                num = current;
            }
            return num;
        }
        /// <summary>返回泛型序列中的最大值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static TSource Max<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            Comparer<TSource> @default = Comparer<TSource>.Default;
            TSource tSource = default(TSource);
            if (tSource == null)
            {
                foreach (TSource current in source)
                {
                    if (current != null && (tSource == null || @default.Compare(current, tSource) > 0))
                    {
                        tSource = current;
                    }
                }
                return tSource;
            }
            bool flag = false;
            foreach (TSource current2 in source)
            {
                if (flag)
                {
                    if (@default.Compare(current2, tSource) > 0)
                    {
                        tSource = current2;
                    }
                }
                else
                {
                    tSource = current2;
                    flag = true;
                }
            }
            if (flag)
            {
                return tSource;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最大 <see cref="T:System.Int32" /> 值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Int32" /> 的最大值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Int32&gt; 的值或 Visual Basic 中与序列中最大值对应的 Nullable(Of Int32)。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最大 <see cref="T:System.Int64" /> 值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Int64" /> 的最大值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Int64&gt; 的值或 Visual Basic 中与序列中最大值对应的 Nullable(Of Int64)。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最大 <see cref="T:System.Single" /> 值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Single" /> 的最大值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Single&gt; 的值或 Visual Basic 中与序列中最大值对应的 Nullable(Of Single)。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static float? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最大 <see cref="T:System.Double" /> 值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static double Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Double" /> 的最大值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Double&gt; 的值或 Visual Basic 中与序列中最大值对应的 Nullable(Of Double)。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static double? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回最大 <see cref="T:System.Decimal" /> 值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static decimal Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用序列的每个元素上的转换函数并返回可空 <see cref="T:System.Decimal" /> 的最大值。</summary>
        /// <returns>C# 中类型为 Nullable&lt;Decimal&gt; 的值或 Visual Basic 中与序列中最大值对应的 Nullable(Of Decimal)。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static decimal? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Max();
        }
        /// <summary>调用泛型序列的每个元素上的转换函数并返回最大结果值。</summary>
        /// <returns>序列中的最大值。</returns>
        /// <param name="source">要确定其最大值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <typeparam name="TResult">
        ///   <paramref name="selector" /> 返回的值的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static TResult Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            return source.Select(selector).Max<TResult>();
        }
        /// <summary>计算 <see cref="T:System.Int32" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算平均值的 <see cref="T:System.Int32" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double Average(this IEnumerable<int> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                long num2 = 0L;
                foreach (int current in source)
                {
                    num += unchecked((long)current);
                    num2 += 1L;
                }
                if (num2 > 0L)
                {
                    return (double)num / (double)num2;
                }
                throw new InvalidOperationException("NoElements"); ;
            }
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int32" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算平均值的可以为 null 的 <see cref="T:System.Int32" />值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double? Average(this IEnumerable<int?> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                long num2 = 0L;
                foreach (int? current in source)
                {
                    if (current.HasValue)
                    {
                        num += unchecked((long)current.GetValueOrDefault());
                        num2 += 1L;
                    }
                }
                if (num2 > 0L)
                {
                    return new double?((double)num / (double)num2);
                }
                return null;
            }
        }
        /// <summary>计算 <see cref="T:System.Int64" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算平均值的 <see cref="T:System.Int64" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double Average(this IEnumerable<long> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                long num2 = 0L;
                foreach (long current in source)
                {
                    num += current;
                    num2 += 1L;
                }
                if (num2 > 0L)
                {
                    return (double)num / (double)num2;
                }
                throw new InvalidOperationException("NoElements"); ;
            }
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int64" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算平均值的可以为 null 的 <see cref="T:System.Int64" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double? Average(this IEnumerable<long?> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                long num = 0L;
                long num2 = 0L;
                foreach (long? current in source)
                {
                    if (current.HasValue)
                    {
                        num += current.GetValueOrDefault();
                        num2 += 1L;
                    }
                }
                if (num2 > 0L)
                {
                    return new double?((double)num / (double)num2);
                }
                return null;
            }
        }
        /// <summary>计算 <see cref="T:System.Single" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算平均值的 <see cref="T:System.Single" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static float Average(this IEnumerable<float> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            long num2 = 0L;
            foreach (float num3 in source)
            {
                num += (double)num3;
                checked
                {
                    num2 += 1L;
                }
            }
            if (num2 > 0L)
            {
                return (float)(num / (double)num2);
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Single" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算平均值的可以为 null 的 <see cref="T:System.Single" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static float? Average(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            long num2 = 0L;
            foreach (float? current in source)
            {
                if (current.HasValue)
                {
                    num += (double)current.GetValueOrDefault();
                    checked
                    {
                        num2 += 1L;
                    }
                }
            }
            if (num2 > 0L)
            {
                return new float?((float)(num / (double)num2));
            }
            return null;
        }
        /// <summary>计算 <see cref="T:System.Double" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算平均值的 <see cref="T:System.Double" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static double Average(this IEnumerable<double> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            long num2 = 0L;
            foreach (double num3 in source)
            {
                num += num3;
                checked
                {
                    num2 += 1L;
                }
            }
            if (num2 > 0L)
            {
                return num / (double)num2;
            }
            throw new InvalidOperationException("NoElements"); ;
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Double" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算平均值的可以为 null 的 <see cref="T:System.Double" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        public static double? Average(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            double num = 0.0;
            long num2 = 0L;
            foreach (double? current in source)
            {
                if (current.HasValue)
                {
                    num += current.GetValueOrDefault();
                    checked
                    {
                        num2 += 1L;
                    }
                }
            }
            if (num2 > 0L)
            {
                return new double?(num / (double)num2);
            }
            return null;
        }
        /// <summary>计算 <see cref="T:System.Decimal" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算平均值的 <see cref="T:System.Decimal" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal Average(this IEnumerable<decimal> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                decimal d = 0m;
                long num = 0L;
                foreach (decimal current in source)
                {
                    d += current;
                    num += 1L;
                }
                if (num > 0L)
                {
                    return d / num;
                }
                throw new InvalidOperationException("NoElements"); ;
            }
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Decimal" /> 值序列的平均值。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算平均值的可以为 null 的 <see cref="T:System.Decimal" /> 值序列。</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal? Average(this IEnumerable<decimal?> source)
        {
            checked
            {
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                decimal d = 0m;
                long num = 0L;
                foreach (decimal? current in source)
                {
                    if (current.HasValue)
                    {
                        d += current.GetValueOrDefault();
                        num += 1L;
                    }
                }
                if (num > 0L)
                {
                    return new decimal?(d / num);
                }
                return null;
            }
        }
        /// <summary>计算 <see cref="T:System.Int32" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int32" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算 <see cref="T:System.Int64" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">source 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Int64.MaxValue" />。</exception>
        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Int64" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算 <see cref="T:System.Single" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Single" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算 <see cref="T:System.Double" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Double" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算 <see cref="T:System.Decimal" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值。</returns>
        /// <param name="source">用于计算平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.InvalidOperationException">
        ///   <paramref name="source" /> 中不包含任何元素。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            return source.Select(selector).Average();
        }
        /// <summary>计算可以为 null 的 <see cref="T:System.Decimal" /> 值序列的平均值，该值可通过调用输入序列的每个元素的转换函数获取。</summary>
        /// <returns>值序列的平均值；如果源序列为空或仅包含为 null 的值，则为 null。</returns>
        /// <param name="source">要计算其平均值的值序列。</param>
        /// <param name="selector">应用于每个元素的转换函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="selector" /> 为 null。</exception>
        /// <exception cref="T:System.OverflowException">序列中元素之和大于 <see cref="F:System.Decimal.MaxValue" />。</exception>
        public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            return source.Select(selector).Average();
        }
        #endregion
    }
}
#pragma warning restore 1734
#endif