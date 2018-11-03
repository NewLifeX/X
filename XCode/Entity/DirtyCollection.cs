using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace XCode
{
    /// <summary>脏属性集合</summary>
    /// <remarks>
    /// 脏数据需要并行高性能，要节省内存，允许重复。
    /// 普通集合加锁成本太高，并行集合内存消耗太大，并行字典只有一两项的时候也要占用7.9k内存。
    /// </remarks>
    [Serializable]
    public class DirtyCollection : IEnumerable<String>
    {
        private String[] _items = new String[8];
        private Int32 _size;
        private Int32 _length;

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
            if (Contains(item)) return;

            // 抢位置
            var n = Interlocked.Increment(ref _length);

            var ms = _items;
            while (ms.Length < _length)
            {
                // 扩容
                var arr = new String[ms.Length * 2];
                Array.Copy(ms, arr, ms.Length);
                if (Interlocked.CompareExchange(ref _items, arr, ms) == ms) break;

                ms = _items;
            }

            _items[n - 1] = item;

            Interlocked.Increment(ref _size);
        }

        private void Remove(String item)
        {
            var len = _length;
            var ms = _items;
            if (len > ms.Length) len = ms.Length;
            for (var i = 0; i < len; i++)
            {
                if (ms[i] == item)
                {
                    ms[i] = null;

                    Interlocked.Decrement(ref _size);
                }
            }
        }

        private Boolean Contains(String item)
        {
            var len = _length;
            var ms = _items;
            if (len > ms.Length) len = ms.Length;
            for (var i = 0; i < len; i++)
            {
                if (ms[i] == item) return true;
            }

            return false;
        }

        /// <summary>清空</summary>
        public void Clear()
        {
            _length = 0;
            _size = 0;
            Array.Clear(_items, 0, _items.Length);
        }

        /// <summary>个数</summary>
        public Int32 Count => _size;

        /// <summary>枚举迭代</summary>
        /// <returns></returns>
        public IEnumerator<String> GetEnumerator()
        {
            var len = _length;
            var ms = _items;
            if (len > ms.Length) len = ms.Length;
            for (var i = 0; i < len; i++)
            {
                if (ms[i] != null) yield return ms[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}