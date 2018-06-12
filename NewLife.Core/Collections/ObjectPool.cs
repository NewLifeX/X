using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NewLife.Reflection;

namespace NewLife.Collections
{
    /// <summary>对象池。数组实现，高性能</summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> : IPool<T> where T : class
    {
        #region 属性
        /// <summary>对象池大小</summary>
        public Int32 Size { get; }

        private readonly Item[] _items;
        private T _current;

        struct Item
        {
            public T Value;
        }
        #endregion

        #region 方法
        /// <summary>实例化对象池。默认大小CPU*2</summary>
        /// <param name="size"></param>
        public ObjectPool(Int32 size = 0)
        {
            if (size <= 0) size = Environment.ProcessorCount * 2;

            Size = size;
            _items = new Item[size - 1];
        }

        /// <summary>获取</summary>
        /// <returns></returns>
        public virtual T Get()
        {
            var val = _current;
            if (val != null && Interlocked.CompareExchange(ref _current, null, val) == val) return val;

            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                val = items[i].Value;
                if (val != null && Interlocked.CompareExchange(ref items[i].Value, null, val) == val) return val;
            }

            return OnCreate();
        }

        /// <summary>归还</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Return(T value)
        {
            if (_current == null && Interlocked.CompareExchange(ref _current, value, null) == null) return true;

            var items = _items;
            for (var i = 0; i < items.Length; ++i)
            {
                if (Interlocked.CompareExchange(ref items[i].Value, value, null) == null) return true;
            }

            return false;
        }

        /// <summary>清空</summary>
        /// <returns></returns>
        public Int32 Clear()
        {
            var count = 0;

            if (_current != null)
            {
                _current = null;
                count++;
            }

            var items = _items;
            for (var i = 0; i < items.Length; ++i)
            {
                if (items[i].Value != null)
                {
                    items[i].Value = null;
                    count++;
                }
            }

            return count;
        }
        #endregion

        #region 重载
        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T OnCreate() => typeof(T).CreateInstance() as T;

        /// <summary>销毁时触发</summary>
        /// <param name="value"></param>
        protected virtual void OnDestroy(T value) => value.TryDispose();
        #endregion
    }
}