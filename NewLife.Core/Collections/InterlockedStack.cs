using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace NewLife.Collections
{
    /// <summary>
    /// 先进先出LIFO的原子锁栈结构，采用CAS保证线程安全。
    /// </summary>
    /// <remarks>
    /// 经过测试，对象数量在万级以上时，性能急剧下降！
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    public class InterlockedStack<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        #region 字段
        /// <summary>
        /// 栈顶
        /// </summary>
        private SingleListNode<T> Top;

        /// <summary>
        /// 版本
        /// </summary>
        private Int32 _version;
        #endregion

        #region 属性
        private Int32 _Count;
        /// <summary>元素个数</summary>
        public Int32 Count
        {
            get { return _Count; }
            //set { _Count = value; }
        }
        #endregion

        #region 核心方法
        //private Int32 maxTimes = 0;
        /// <summary>
        /// 向栈压入一个对象
        /// </summary>
        /// <remarks>重点解决多线程环境下资源争夺以及使用lock造成性能损失的问题</remarks>
        /// <param name="item"></param>
        public void Push(T item)
        {
            SingleListNode<T> newTop = new SingleListNode<T>(item);
            SingleListNode<T> oldTop;
            //Int32 times = 0;
            do
            {
                //times++;
                // 记住当前栈顶
                oldTop = Top;

                // 设置新对象的下一个节点为当前栈顶
                newTop.Next = oldTop;
            }
            // 比较并交换
            // 如果当前栈顶第一个参数的Top等于第三个参数，表明没有被别的线程修改，保存第二参数到第一参数中
            // 否则，不相等表明当前栈顶已经被修改过，操作失败，执行循环
            while (Interlocked.CompareExchange<SingleListNode<T>>(ref Top, newTop, oldTop) != oldTop);

            //if (times > 1) XTrace.WriteLine("命中次数：{0}", times);
            //if (times > maxTimes)
            //{
            //    maxTimes = times;
            //    XTrace.WriteLine("新命中次数：{0}", times);
            //}
            Interlocked.Increment(ref _Count);
            Interlocked.Increment(ref _version);
        }

        /// <summary>
        /// 从栈中弹出一个对象
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            T item;
            if (!TryPop(out item)) throw new InvalidOperationException("栈为空！");

            return item;
        }

        /// <summary>
        /// 尝试从栈中弹出一个对象
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryPop(out T item)
        {
            SingleListNode<T> newTop;
            SingleListNode<T> oldTop;
            //Int32 times = 0;
            do
            {
                //times++;
                // 记住当前栈顶
                oldTop = Top;
                if (oldTop == null)
                {
                    item = default(T);
                    return false;
                }

                // 设置新栈顶为当前栈顶的下一个节点
                newTop = oldTop.Next;
            }
            // 比较并交换
            // 如果当前栈顶第一个参数的Top等于第三个参数，表明没有被别的线程修改，保存第二参数到第一参数中
            // 否则，不相等表明当前栈顶已经被修改过，操作失败，执行循环
            while (Interlocked.CompareExchange<SingleListNode<T>>(ref Top, newTop, oldTop) != oldTop);

            //if (times > 1) XTrace.WriteLine("命中次数：{0}", times);
            //if (times > maxTimes)
            //{
            //    maxTimes = times;
            //    XTrace.WriteLine("新命中次数：{0}", times);
            //}
            Interlocked.Decrement(ref _Count);
            Interlocked.Increment(ref _version);

            item = oldTop.Item;
            return true;
        }

        /// <summary>
        /// 获取栈顶对象，不弹栈
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            T item;
            if (!TryPeek(out item)) throw new InvalidOperationException("栈为空！");

            return item;
        }

        /// <summary>
        /// 尝试获取栈顶对象，不弹栈
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryPeek(out T item)
        {
            SingleListNode<T> top = Top;
            if (top == null)
            {
                item = default(T);
                return false;
            }
            item = top.Item;
            return true;
        }
        #endregion

        #region 集合方法
        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            _Count = 0;
            Top = null;
            Interlocked.Increment(ref _version);
        }

        /// <summary>
        /// 转为数组
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            if (Count < 1) return null;

            T[] arr = new T[Count];
            ((ICollection)this).CopyTo(arr, 0);
            return arr;
        }
        #endregion

        #region ICollection 成员
        void ICollection.CopyTo(Array array, int index)
        {
            if (Top == null || array == null || index >= array.Length) return;

            SingleListNode<T> node = Top;
            while (Top != null && index < array.Length)
            {
                array.SetValue(node.Item, index++);
                node = node.Next;
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        private Object _syncRoot;
        object ICollection.SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    Interlocked.CompareExchange(ref this._syncRoot, new object(), null);
                }
                return this._syncRoot;
            }
        }
        #endregion

        #region IEnumerable 成员
        /// <summary>
        /// 获取枚举器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            //return new Enumerator((InterlockedStack<T>)this);

            for (SingleListNode<T> node = Top; node != null; node = node.Next)
            {
                yield return node.Item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //return new Enumerator((InterlockedStack<T>)this);

            return GetEnumerator();
        }

        ///// <summary>
        ///// 原子栈枚举器
        ///// </summary>
        //[Serializable, StructLayout(LayoutKind.Sequential)]
        //public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        //{
        //    private InterlockedStack<T> _stack;
        //    private int _version;
        //    private Int32 _index;
        //    private SingleListNode<T> current;

        //    internal Enumerator(InterlockedStack<T> stack)
        //    {
        //        _stack = stack;
        //        _version = _stack._version;
        //        _index = -1;
        //        current = null;
        //    }

        //    /// <summary>
        //    /// 释放
        //    /// </summary>
        //    public void Dispose()
        //    {
        //        current = null;
        //    }

        //    /// <summary>
        //    /// 移到下一个
        //    /// </summary>
        //    /// <returns></returns>
        //    public bool MoveNext()
        //    {
        //        if (_version != _stack._version) throw new InvalidOperationException("集合已经被修改！");

        //        if (_index == -1)
        //        {
        //            current = _stack.Top;
        //            _index++;
        //            return true;
        //        }

        //        if (current == null) return false;

        //        current = current.Next;
        //        _index++;
        //        return true;
        //    }

        //    /// <summary>
        //    /// 当前对象
        //    /// </summary>
        //    public T Current
        //    {
        //        get
        //        {
        //            if (current == null) throw new InvalidOperationException("没有开始遍历或遍历已结束！");

        //            return current.Item;
        //        }
        //    }

        //    object IEnumerator.Current
        //    {
        //        get
        //        {
        //            if (current == null) throw new InvalidOperationException("没有开始遍历或遍历已结束！");

        //            return current.Item;
        //        }
        //    }

        //    void IEnumerator.Reset()
        //    {
        //        if (_version != _stack._version) throw new InvalidOperationException("集合已经被修改！");

        //        current = null;
        //        _index = -1;
        //    }
        //}
        #endregion
    }
}