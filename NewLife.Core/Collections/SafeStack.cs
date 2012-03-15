using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NewLife.Threading;

namespace NewLife.Collections
{
    /// <summary>基于数组实现的线程安全栈。快速高效，不会形成内存碎片。</summary>
    /// <remarks>
    /// 链表做的原子栈<see cref="InterlockedStack&lt;T&gt;"/>，本来是为了做对象池用的，但是链表节点自身也会形成内存碎片，给GC压力，十分纠结。
    /// 一直认为用数组做存储是效率最好的，但是纠结于无法实现原子操作，而迟迟不敢动手。
    /// 
    /// 最好指定初始容量，因为采用数组作为存储结构最大的缺点就是容量固定，从而导致满存储时必须重新分配数组，并且复制。
    /// 
    /// 在 @Aimeast 的指点下，有所感悟，我们没必要严格的追求绝对安全，只要把冲突可能性降到尽可能低即可。
    /// 
    /// 安全栈有问题，如果在同一个位置同时压入和弹出，可能会导致这个位置为空，后面再弹出的时候，只得到空值。
    /// 通过增加一个锁定数组来解决这个问题，锁定数组实现不严格的数字对比锁定，保证性能。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class SafeStack<T> : DisposeBase, IStack<T>, IEnumerable<T>, ICollection, IEnumerable where T : class
    {
        #region 属性
        /// <summary>数据数组。用于存放对象。</summary>
        private T[] _array;

        private Int32 _Count;
        /// <summary>元素个数，同时也是下一个空位的位置指针</summary>
        public Int32 Count { get { return _Count; } }

        /// <summary>最大容量</summary>
        public Int32 Capacity { get { return _array.Length; } }
        #endregion

        #region 构造
        /// <summary>实例化一个容纳4个元素的安全栈</summary>
        public SafeStack() : this(256) { }

        /// <summary>实例化一个指定大小的安全栈</summary>
        /// <param name="capacity"></param>
        public SafeStack(Int32 capacity) { _array = new T[capacity]; }

        /// <summary>使用指定枚举实例化一个安全栈</summary>
        /// <param name="collection"></param>
        public SafeStack(IEnumerable collection)
        {
            var list = new List<T>();
            foreach (var item in collection)
            {
                list.Add((T)item);
            }
            _array = list.ToArray();
            _Count = _array.Length;
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            for (int i = 0; i < _array.Length && i < Count; i++)
            {
                var item = _array[i];
                if (item != null && item is IDisposable) (item as IDisposable).Dispose();
                _array[i] = default(T);
            }
            _array = null;
        }
        #endregion

        #region 核心方法
        Int32 total = 0;
        Int32 times = 0;

        /// <summary>平均</summary>
        public Int32 Avg { get { return times == 0 ? 0 : total / times; } }

        /// <summary>向栈压入一个对象</summary>
        /// <remarks>重点解决多线程环境下资源争夺以及使用lock造成性能损失的问题</remarks>
        /// <param name="item"></param>
        public void Push(T item)
        {
            var len = _array.Length;
            // 从head开始，循环遍历数组
            var head = LastFree;
            for (int i = 0; i < len; i++, len = _array.Length)
            {
                if (_Count >= len) CheckSize();

                var k = i + head;
                if (k >= len) k -= len;

                // 尝试交换，成功后返回
                if (_array[k] == null && Interlocked.CompareExchange<T>(ref _array[k], item, null) == null)
                {
                    // 记录位置，下一次从这里开始找
                    LastNotFree = k;
                    Interlocked.Increment(ref _Count);
                    LastFree = k + 1;
                    total += i + 1;
                    times++;
                    return;
                }
            }
            total += len;
            times++;
            throw new Exception("Error On Push");
        }

        Object _lock_CheckSize = new Object();
        void CheckSize()
        {
            // 如果当前已经用完，那么马上扩容。当然，有可能扩容前就被别人抢到后面的位置，所以，这里要尽快加锁
            if (_Count < _array.Length) return;
            lock (_lock_CheckSize)
            {
                if (_Count < _array.Length) return;

                // 稍等一会，可能某些读取尚未完成
                Thread.SpinWait(100);

                // 以4为最小值，成倍扩容
                Int32 size = _array.Length < 4 ? 4 : _array.Length * 2;
                var _arr = new T[size];
                _array.CopyTo(_arr, 0);
                _array = _arr;
            }
        }

        /// <summary>从栈中弹出一个对象</summary>
        /// <returns></returns>
        public T Pop()
        {
            T item;
            if (!TryPop(out item)) throw new InvalidOperationException("栈为空！");

            return item;
        }

        /// <summary>尝试从栈中弹出一个对象</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryPop(out T item)
        {
            var len = _array.Length;
            if (_Count >= len) CheckSize();

            // 从tails开始，反向循环遍历数组
            var tails = LastNotFree;
            for (int i = len - 1; i >= 0; i--)
            {
                var k = i + tails + 1;
                if (k >= len) k -= len;

                // 先判断一次再交换，掠夺式获取数组元素，Exchange比CompareExchange更轻量级
                if (_array[k] != null && (item = Interlocked.Exchange<T>(ref _array[k], null)) != null)
                {
                    // 记录位置，下一次从这里开始找
                    LastFree = k;
                    Interlocked.Decrement(ref _Count);
                    total += len - i;
                    times++;
                    return true;
                }
            }

            item = null;
            return false;
        }

        // 记录最后的空闲位置和非空闲位置，一级缓存
        const Int32 SIZE_OF_CACHE = 100000;
        // 指向下一个空位置
        volatile Int32 lastFreeIndex = 0;
        volatile Int32 lastNotFreeIndex = 0;
        volatile Int32[] lastFree = new Int32[SIZE_OF_CACHE];
        volatile Int32[] lastNotFree = new Int32[SIZE_OF_CACHE];

        Int32 LastFree
        {
            get
            {
                var k = lastFreeIndex;
                if (k <= 0) return 0;
                lastFreeIndex--;
                return lastFree[k - 1];
            }
            set
            {
                var k = lastFreeIndex;
                if (k >= SIZE_OF_CACHE) return;
                lastFreeIndex++;
                lastFree[k] = value;
            }
        }

        Int32 LastNotFree
        {
            get
            {
                var k = lastNotFreeIndex;
                if (k <= 0) return 0;
                lastNotFreeIndex--;
                return lastNotFree[k - 1];
            }
            set
            {
                var k = lastNotFreeIndex;
                if (k >= SIZE_OF_CACHE) return;
                lastNotFreeIndex++;
                lastNotFree[k] = value;
            }
        }
        #endregion

        #region 集合方法
        /// <summary>清空</summary>
        public void Clear()
        {
            // 先把指针移到开头，再执行清空操作，减少冲突可能性
            var len = Count;
            _Count = 0;
            for (int i = 0; i < _array.Length && i < len; i++) _array[i] = default(T);
        }

        /// <summary>转为数组</summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            var len = Count;
            if (len < 1) return null;

            T[] arr = new T[len];
            Array.Copy(_array, 0, arr, 0, len);
            return arr;
        }
        #endregion

        #region ICollection 成员
        void ICollection.CopyTo(Array array, int index)
        {
            if (Count < 1 || array == null || index >= array.Length) return;

            //_array.CopyTo(array, index);
            Array.Copy(_array, 0, array, index, Count);
        }

        bool ICollection.IsSynchronized { get { return true; } }

        private Object _syncRoot;
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }
        #endregion

        #region IEnumerable 成员
        /// <summary>获取枚举器</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _array.Length && i < Count; i++) yield return _array[i];
        }

        IEnumerator IEnumerable.GetEnumerator() { return _array.GetEnumerator(); }
        #endregion
    }
}