using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace NewLife.Collections
{
    /// <summary>基于lock的安全栈</summary>
    /// <remarks>
    /// 理论上性能最差，但实际测试发现，似乎比InterlockedStack要好点。
    /// 重点是它安全可信。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class LockStack<T> : IStack<T>
    {
        Stack<T> stack = new Stack<T>();

        #region IStack<T> 成员
        /// <summary>向栈压入一个对象</summary>
        /// <param name="item"></param>
        public void Push(T item)
        {
            lock (stack)
            {
                stack.Push(item);
            }
        }

        /// <summary>从栈中弹出一个对象</summary>
        /// <returns></returns>
        public T Pop()
        {
            lock (stack)
            {
                return stack.Pop();
            }
        }

        /// <summary>尝试从栈中弹出一个对象</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryPop(out T item)
        {
            item = default(T);
            if (stack.Count < 1) return false;
            lock (stack)
            {
                if (stack.Count < 1) return false;
                item = stack.Pop();
                return true;
            }
        }

        /// <summary>获取栈顶对象，不弹栈</summary>
        /// <returns></returns>
        public T Peek()
        {
            lock (stack)
            {
                return stack.Peek();
            }
        }

        /// <summary>尝试获取栈顶对象，不弹栈</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryPeek(out T item)
        {
            item = default(T);
            if (stack.Count < 1) return false;
            lock (stack)
            {
                if (stack.Count < 1) return false;
                item = stack.Peek();
                return true;
            }
        }

        #endregion

        #region IEnumerable<T> 成员
        /// <summary>获取枚举器</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() { return stack.GetEnumerator(); }
        #endregion

        #region IEnumerable 成员
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        #endregion

        #region ICollection 成员
        void ICollection.CopyTo(Array array, int index) { (stack as ICollection).CopyTo(array, index); }

        int ICollection.Count { get { return stack.Count; } }

        bool ICollection.IsSynchronized { get { return true; } }

        object ICollection.SyncRoot { get { return (stack as ICollection).SyncRoot; } }
        #endregion

        #region IDisposable 成员
        void IDisposable.Dispose() { stack.Clear(); }
        #endregion
    }
}