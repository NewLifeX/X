using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NewLife.Net.Common
{
    /// <summary>
    /// 对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pool<T> : IDisposable where T : new()
    {
        #region 属性
        private Stack<T> _Stock;
        /// <summary>在库</summary>
        private Stack<T> Stock
        {
            get { return _Stock ?? (_Stock = new Stack<T>(Max)); }
        }

        //private List<T> _NotStock;
        ///// <summary>不在库</summary>
        //private List<T> NotStock
        //{
        //    get { return _NotStock ?? (_NotStock = new List<T>()); }
        //}

        private Int32 _Max = 10000;
        /// <summary>最大缓存数</summary>
        public Int32 Max
        {
            get { return _Max; }
            set { _Max = value; }
        }

        /// <summary>在库</summary>
        public Int32 StockCount { get { return _Stock == null ? 0 : _Stock.Count; } }

        /// <summary>不在库</summary>
        public Int32 NotStockCount { get { return CreateCount - StockCount; } }

        private Int32 _CreateCount;
        /// <summary>创建数</summary>
        public Int32 CreateCount
        {
            get { return _CreateCount; }
            //set { _CreateCount = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 归还
        /// </summary>
        /// <param name="obj"></param>
        public virtual void Push(T obj)
        {
            // 释放后，不再接受归还，因为可能别的线程尝试归还
            if (Disposed) return;

            Stack<T> stack = Stock;

            //// 不是我的，我不要
            //if (!NotStock.Contains(obj)) throw new Exception("不是我的我不要！");
            // 满了，不要
            if (stack.Count > Max) return;
            lock (this)
            {
                if (Disposed) return;
                //if (!NotStock.Contains(obj)) throw new Exception("不是我的我不要！");
                if (stack.Count > Max) return;

                if (stack.Contains(obj)) throw new Exception("设计错误，该对象已经存在于池中！");

                stack.Push(obj);
                //NotStock.Remove(obj);

                //if (CreateCount != StockCount + NotStockCount) throw new Exception("设计错误！");
            }
        }

        /// <summary>
        /// 借出
        /// </summary>
        /// <returns></returns>
        public virtual T Pop()
        {
            Stack<T> stack = Stock;

            lock (this)
            {
                T obj = default(T);
                if (stack.Count > 0)
                {
                    //obj = queue[0];
                    //queue.RemoveAt(0);
                    obj = stack.Pop();
                }
                else
                {
                    obj = Create();
                    Interlocked.Increment(ref _CreateCount);
                }
                //NotStock.Add(obj);

                //if (CreateCount != StockCount + NotStockCount) throw new Exception("设计错误！");
                return obj;
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <returns></returns>
        protected virtual T Create()
        {
            return new T();
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                if (_Stock != null) _Stock.Clear();
                //if (_NotStock != null) _NotStock.Clear();
            }
        }
        #endregion

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>是否已经释放</summary>
        public Boolean Disposed
        {
            get { return disposed > 0; }
        }

        private Int32 disposed = 0;
        /// <summary>
        /// 释放资源，参数表示是否由Dispose调用。该方法保证OnDispose只被调用一次！
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(Boolean disposing)
        {
            if (disposed > 0) return;
            Interlocked.Increment(ref disposed);

            OnDispose(disposing);

            Interlocked.Increment(ref disposed);
        }

        /// <summary>
        /// 子类重载实现资源释放逻辑
        /// </summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected virtual void OnDispose(Boolean disposing)
        {
            if (disposed > 1) throw new Exception("设计错误，OnDispose应该只被调用一次！代码不应该直接调用OnDispose，而应该调用Dispose。");

            if (disposing)
            {
                // 释放托管资源
                if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
                {
                    lock (this)
                    {
                        if (_Stock != null)
                        {
                            foreach (T item in _Stock.ToArray())
                            {
                                ((IDisposable)item).Dispose();
                            }
                            _Stock.Clear();
                        }
                        //if (_NotStock != null)
                        //{
                        //    foreach (T item in _NotStock.ToArray())
                        //    {
                        //        ((IDisposable)item).Dispose();
                        //    }
                        //    _NotStock.Clear();
                        //}
                    }
                }

                // 告诉GC，不要调用析构函数
                GC.SuppressFinalize(this);
            }

            // 释放非托管资源
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~Pool()
        {
            // 如果忘记调用Dispose，这里会释放非托管资源
            // 如果曾经调用过Dispose，因为GC.SuppressFinalize(this)，不会再调用该析构函数
            Dispose(false);
        }
        #endregion
    }
}