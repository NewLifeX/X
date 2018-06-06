using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NewLife.Collections
{
    /// <summary>并行哈希集合</summary>
    /// <remarks>
    /// 主要用于频繁添加删除而又要遍历的场合
    /// </remarks>
    public class ConcurrentHashSet<T> : IEnumerable<T>
    {
        private ConcurrentDictionary<T, Byte> _dic;

        /// <summary>实例化一个并行哈希集合</summary>
        public ConcurrentHashSet()
        {
            _dic = new ConcurrentDictionary<T, Byte>();
        }

        /// <summary>是否空集合</summary>
        public Boolean IsEmpty => _dic.IsEmpty;

        /// <summary>元素个数</summary>
        public Int32 Count => _dic.Count;

        /// <summary>是否包含元素</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contain(T item) => _dic.ContainsKey(item);

        /// <summary>尝试添加</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryAdd(T item) => _dic.TryAdd(item, 0);

        /// <summary>尝试删除</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryRemove(T item) => _dic.TryRemove(item, out var b);

        #region IEnumerable<T> 成员
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _dic.Keys.GetEnumerator();
        #endregion

        #region IEnumerable 成员
        IEnumerator IEnumerable.GetEnumerator() => _dic.Keys.GetEnumerator();
        #endregion
    }
}