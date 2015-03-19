using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NET45PLUS
using System.Runtime.CompilerServices;
#endif

namespace System.Collections.Generic
{
    /// <summary>集合扩展</summary>
    public static class CaCollectionExtensions
    {
        /// <summary>将当前集合中的元素转换为另一种类型，并返回包含转换后的元素的集合。</summary>
        /// <typeparam name="T">集合中元素类型</typeparam>
        /// <typeparam name="TResult">目标集合元素的类型</typeparam>
        /// <param name="items">原集合</param>
        /// <param name="transformation">将每个元素从一种类型转换为另一种类型的委托</param>
        /// <returns></returns>
#if NET45PLUS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static IEnumerable<TResult> ConvertAllX<T, TResult>(this IEnumerable<T> items, Converter<T, TResult> transformation)
        {
            var arr = items as T[];
            if (arr != null)
            {
                return Array.ConvertAll(arr, transformation);
            }
            var list = items as List<T>;
            if (list != null)
            {
                return list.ConvertAll(transformation);
            }

            return items.Select(_ => transformation(_));
        }

        /// <summary>对集合中的每个元素执行指定操作</summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="items">指定类型的集合</param>
        /// <param name="action">是对传递给它的对象执行某个操作的方法的委托</param>
#if NET45PLUS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static void ForEachX<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (items == null) { return; }

            var arr = items as T[];
            if (arr != null)
            {
                Array.ForEach(arr, action);
                return;
            }
            var list = items as List<T>;
            if (list != null)
            {
                list.ForEach(action);
                return;
            }
            foreach (var item in items)
            {
                action(item);
            }
        }

        /// <summary>搜索与指定谓词所定义的条件相匹配的元素，并返回整个 Array 中的第一个匹配元素。</summary>
        /// <typeparam name="T">数组元素的类型</typeparam>
        /// <param name="items">要搜索的从零开始的一维数组</param>
        /// <param name="predicate">定义要搜索的元素的条件</param>
        /// <returns>如果找到与指定谓词定义的条件匹配的第一个元素，则为该元素；否则为类型 T 的默认值。</returns>
        /// <remarks>
        /// Code taken from Castle Project's Castle.Core Library
        /// &lt;a href="http://www.castleproject.org/"&gt;Castle Project's Castle.Core Library&lt;/a&gt;
        /// </remarks>
#if NET45PLUS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static T Find<T>(this T[] items, Predicate<T> predicate)
        {
            return Array.Find(items, predicate);
        }

        /// <summary>检索与指定谓词定义的条件匹配的所有元素</summary>
        /// <typeparam name="T">数组元素的类型</typeparam>
        /// <param name="items">要搜索的从零开始的一维数组</param>
        /// <param name="predicate">定义要搜索的元素的条件</param>
        /// <returns>如果找到一个其中所有元素均与指定谓词定义的条件匹配的数组，则为该数组；否则为一个空数组。</returns>
        /// <remarks>
        /// Code taken from Castle Project's Castle.Core Library
        /// &lt;a href="http://www.castleproject.org/"&gt;Castle Project's Castle.Core Library&lt;/a&gt;
        /// </remarks>
#if NET45PLUS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static T[] FindAll<T>(this T[] items, Predicate<T> predicate)
        {
            return Array.FindAll(items, predicate);
        }

        /// <summary>Checks whether or not collection is null or empty. Assumes colleciton can be safely enumerated multiple times.</summary>
        /// <param name = "this"></param>
        /// <returns></returns>
        /// <remarks>
        /// Code taken from Castle Project's Castle.Core Library
        /// &lt;a href="http://www.castleproject.org/"&gt;Castle Project's Castle.Core Library&lt;/a&gt;
        /// </remarks>
#if NET45PLUS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static Boolean IsNullOrEmpty(this IEnumerable @this)
        {
            return @this == null || @this.GetEnumerator().MoveNext() == false;
        }
    }
}
