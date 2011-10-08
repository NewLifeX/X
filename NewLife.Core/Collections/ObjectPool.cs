using System;
using System.Threading;
using NewLife.Reflection;

namespace NewLife.Collections
{
    /// <summary>
    /// 对象池。采用原子栈设计，避免锁资源的争夺。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> : DisposeBase //where T : new()
    {
        #region 属性
        private InterlockedStack<T> _Stock;
        /// <summary>在库</summary>
        private InterlockedStack<T> Stock
        {
            get { return _Stock ?? (_Stock = new InterlockedStack<T>()); }
        }

        //private List<T> _NotStock;
        ///// <summary>不在库</summary>
        //private List<T> NotStock
        //{
        //    get { return _NotStock ?? (_NotStock = new List<T>()); }
        //}

        //private Int32 _Max = 10000;
        ///// <summary>最大缓存数</summary>
        //public Int32 Max
        //{
        //    get { return _Max; }
        //    set { _Max = value; }
        //}

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

        #region 事件
        /// <summary>
        /// 对象创建委托。在对象池内对象不足时调用，如未设置，则调用类型的默认构造函数创建对象。
        /// </summary>
        public Func<T> OnCreate;
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

            InterlockedStack<T> stack = Stock;

            //// 不是我的，我不要
            //if (!NotStock.Contains(obj)) throw new Exception("不是我的我不要！");
            //// 满了，不要
            //if (stack.Count > Max) return;

            //if (stack.Contains(obj)) throw new Exception("设计错误，该对象已经存在于池中！");

            stack.Push(obj);
            //NotStock.Remove(obj);

            //if (CreateCount != StockCount + NotStockCount) throw new Exception("设计错误！");
        }

        /// <summary>
        /// 借出
        /// </summary>
        /// <returns></returns>
        public virtual T Pop()
        {
            InterlockedStack<T> stack = Stock;

            T obj;
            if (stack.TryPop(out obj)) return obj;

            obj = Create();
            Interlocked.Increment(ref _CreateCount);
            //NotStock.Add(obj);

            //if (CreateCount != StockCount + NotStockCount) throw new Exception("设计错误！");
            return obj;
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <returns></returns>
        protected virtual T Create()
        {
            //return new T();

            if (OnCreate != null) return OnCreate();

            return (T)TypeX.CreateInstance(typeof(T));
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            if (_Stock != null) _Stock.Clear();
            //if (_NotStock != null) _NotStock.Clear();
        }
        #endregion

        #region 释放资源
        /// <summary>
        /// 子类重载实现资源释放逻辑
        /// </summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(Boolean disposing)
        {
            if (disposing)
            {
                // 释放托管资源
                Clear();
            }

            base.OnDispose(disposing);
        }
        #endregion
    }
}