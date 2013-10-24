#if !NET4
using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Reflection;

namespace System.Linq
{
    /// <summary>表示已排序序列。</summary>
    /// <typeparam name="TElement">序列中的元素的类型。</typeparam>
    /// <filterpriority>2</filterpriority>
    public interface IOrderedEnumerable<TElement> : IEnumerable<TElement>, IEnumerable
    {
        /// <summary>根据某个键对 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" /> 的元素执行后续排序。</summary>
        /// <returns>一个 <see cref="T:NewLife.Linq.IOrderedEnumerable`1" />，其元素按键进行排序。</returns>
        /// <param name="keySelector">用于提取每个元素的键的 <see cref="T:System.Func`2" />。</param>
        /// <param name="comparer">用于比较键在返回序列中的位置的 <see cref="T:System.Collections.Generic.IComparer`1" />。</param>
        /// <param name="descending">如果为 true，则对元素进行降序排序；如果为 false，则对元素进行升序排序。</param>
        /// <typeparam name="TKey">
        ///   <paramref name="keySelector" /> 生成的键的类型。</typeparam>
        /// <filterpriority>2</filterpriority>
        IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending);
    }
}
#endif