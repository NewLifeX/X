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
            _dic = new ConcurrentDictionary<T, byte>();
        }

        /// <summary>是否空集合</summary>
        public Boolean IsEmpty { get { return _dic.IsEmpty; } }

        /// <summary>元素个数</summary>
        public Int32 Count { get { return _dic.Count; } }

        /// <summary>是否包含元素</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contain(T item) { return _dic.ContainsKey(item); }

        /// <summary>尝试添加</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryAdd(T item) { return _dic.TryAdd(item, 0); }

        /// <summary>尝试删除</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryRemove(T item)
        {
            Byte b = 0;
            return _dic.TryRemove(item, out b);
        }

        #region IEnumerable<T> 成员

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _dic.Keys.GetEnumerator();
        }

        #endregion

        #region IEnumerable 成员

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dic.Keys.GetEnumerator();
        }

        #endregion
    }
}