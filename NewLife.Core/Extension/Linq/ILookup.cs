#if !NET4
using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>定义索引器、大小属性以及将键映射到 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 值序列的数据结构的布尔搜索方法。</summary>
    /// <typeparam name="TKey">
    ///   <see cref="T:NewLife.Linq.ILookup`2" /> 中的键的类型。</typeparam>
    /// <typeparam name="TElement">组成 <see cref="T:NewLife.Linq.ILookup`2" /> 中的值的 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 序列中的元素的类型。</typeparam>
    /// <filterpriority>2</filterpriority>
    public interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
    {
        /// <summary>Gets the number of key/value collection pairs in the <see cref="T:NewLife.Linq.ILookup`2" />.</summary>
        /// <returns>The number of key/value collection pairs in the <see cref="T:NewLife.Linq.ILookup`2" />.</returns>
        int Count
        {
            get;
        }
        /// <summary>获取按指定键进行索引的值的 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 序列。</summary>
        /// <returns>按指定键进行索引的值的 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 序列。</returns>
        /// <param name="key">所需值序列的键。</param>
        IEnumerable<TElement> this[TKey key]
        {
            get;
        }
        /// <summary>确定指定的键是否位于 <see cref="T:NewLife.Linq.ILookup`2" /> 中。</summary>
        /// <returns>如果 <paramref name="key" /> 在 <see cref="T:NewLife.Linq.ILookup`2" /> 中，则为 true；否则为 false。</returns>
        /// <param name="key">要在 <see cref="T:NewLife.Linq.ILookup`2" /> 中搜索的键。</param>
        bool Contains(TKey key);
    }
}
#endif