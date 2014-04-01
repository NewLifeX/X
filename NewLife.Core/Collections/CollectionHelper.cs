using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>集合扩展</summary>
    public static class CollectionHelper
    {
        /// <summary>集合转为数组</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static T[] ToArray<T>(this ICollection<T> collection, Int32 index = 0)
        {
            var arr = new T[collection.Count];
            collection.CopyTo(arr, index);
            return arr;
        }
    }
}