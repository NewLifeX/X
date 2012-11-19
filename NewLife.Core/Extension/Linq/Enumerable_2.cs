using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Reflection;

namespace NewLife.Linq
{
#if NET4
    /// <summary>提供一组用于查询实现 <see cref="T:System.Collections.Generic.IEnumerable`1" /> 的对象的 static（在 Visual Basic 中为 Shared）方法。</summary>
#endif
    public static partial class Enumerable
    {
        /// <summary>遍历序列中的所有元素。</summary>
        /// <param name="source">包含要应用谓词的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("因为.Net4.0不支持，有点鸡肋，将来不支持！")]
        public static IEnumerable<TSource> ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> predicate)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            foreach (TSource current in source)
            {
                predicate(current);
            }
            return source;
        }

        /// <summary>遍历序列中的所有元素。</summary>
        /// <param name="source">包含要应用谓词的元素的 <see cref="T:System.Collections.Generic.IEnumerable`1" />。</param>
        /// <param name="predicate">用于测试每个元素是否满足条件的函数。</param>
        /// <typeparam name="TSource">
        ///   <paramref name="source" /> 中的元素的类型。</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="source" /> 或 <paramref name="predicate" /> 为 null。</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("因为.Net4.0不支持，有点鸡肋，将来不支持！")]
        public static IEnumerable<TSource> ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource, int> predicate)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            Int32 i = 0;
            foreach (TSource current in source)
            {
                predicate(current, i++);
            }
            return source;
        }
    }
}