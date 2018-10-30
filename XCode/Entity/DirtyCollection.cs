using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace XCode
{
    /// <summary>脏属性集合</summary>
    /// <remarks>脏数据需要并行高性能，要节省内存，允许重复</remarks>
    [Serializable]
    public class DirtyCollection : IEnumerable<String>
    {
        private String[] _items = new String[8];
        private Int32 _size;
        private Int32 _size2;

        /// <summary>获取或设置与指定的属性是否有脏数据。</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean this[String item]
        {
            get => Contains(item);
            set
            {
                if (value)
                    Add(item);
                else
                    Remove(item);
            }
        }

        private void Add(String item)
        {
            // 抢位置
            var n = Interlocked.Increment(ref _size2);

            if (_items.Length <= n)
            {
                // 加锁扩容
                lock (this)
                {
                    if (_items.Length <= n) EnsureCapacity(n + 1);
                }
            }

            n = Interlocked.Increment(ref _size);
            _items[n] = item;
        }

        private void EnsureCapacity(Int32 min)
        {
            var len = _items.Length * 2;
            if (len < min) len = min;

            // 扩容
            var array = new String[len];
            if (_size > 0) Array.Copy(_items, 0, array, 0, _size);

            _items = array;
        }

        private void Remove(String item)
        {
            var ms = _items;
            if (ms.Length == 0) return;

            var s = _size;
            for (var i = s - 1; i >= 0; i--)
            {
                if (ms[i] == item)
                {
                    // 把最后一个换过来
                    if (i < s - 1)
                        ms[i] = ms[s - 1];
                    else
                        ms[i] = null;

                    Interlocked.Decrement(ref _size);
                }
            }
        }

        private Boolean Contains(String item)
        {
            for (var i = 0; i < _size; i++)
            {
                if (_items[i] == item) return true;
            }

            return false;
        }

        /// <summary>清空</summary>
        public void Clear() => _size = 0;

        /// <summary>是否有任意一个</summary>
        /// <returns></returns>
        public Boolean Any() => _size > 0;

        /// <summary>转数组</summary>
        /// <returns></returns>
        public String[] ToArray()
        {
            var arr = new String[_size];
            if (arr.Length > 0) Array.Copy(_items, 0, arr, 0, arr.Length);

            return arr;
        }

        /// <summary>枚举迭代</summary>
        /// <returns></returns>
        public IEnumerator<String> GetEnumerator()
        {
            for (var i = 0; i < _size; i++)
            {
                yield return _items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}