using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Collections
{
    /// <summary>对象池。采用原子栈设计，避免锁资源的争夺。</summary>
    /// <remarks>
    /// 经过测试，对象数量在万级以上时，性能下降很快！
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T> : DisposeBase //where T : new()
    {
        #region 属性
#if NET4
        private ConcurrentStack<T> _Stock;
        /// <summary>在库</summary>
        public ConcurrentStack<T> Stock { get { return _Stock; } set { _Stock = value; } }
#else
        private IStack<T> _Stock;
        /// <summary>在库</summary>
        public IStack<T> Stock { get { return _Stock; } set { _Stock = value; } }
#endif

        private Int32 _Max = 1000;
        /// <summary>最大缓存数。默认1000，超过后将启用定时器来清理</summary>
        public Int32 Max { get { return _Max; } set { _Max = value; } }

        /// <summary>在库</summary>
        public Int32 StockCount { get { return _Stock.Count; } }

        /// <summary>不在库</summary>
        public Int32 NotStockCount { get { return CreateCount - StockCount - FreeCount; } }

        private Int32 _FreeCount;
        /// <summary>被释放的对象数</summary>
        public Int32 FreeCount { get { return _FreeCount; } set { _FreeCount = value; } }

        private Int32 _CreateCount;
        /// <summary>创建数</summary>
        public Int32 CreateCount { get { return _CreateCount; } }
        #endregion

        #region 构造
        /// <summary>实例化一个对象池</summary>
        public ObjectPool() { Stock = new ConcurrentStack<T>(); }
        #endregion

        #region 事件
        /// <summary>对象创建委托。在对象池内对象不足时调用，如未设置，则调用类型的默认构造函数创建对象。</summary>
        public Func<T> OnCreate;
        #endregion

        #region 方法
        /// <summary>归还</summary>
        /// <param name="obj"></param>
        public virtual void Push(T obj)
        {
            // 释放后，不再接受归还，因为可能别的线程尝试归还
            if (Disposed) return;

            var stack = Stock;

            // 超过最大值了，启动清理定时器
            if (stack.Count > Max)
            {
                if (_clearTimers == null) _clearTimers = new TimerX(ClearAboveMax, null, 0, XTrace.Debug ? 10000 : 120000);
            }
            else
            {
                // 没到最大值，关闭定时器？
                if (stack.Count < Max - 100 && _clearTimers != null)
                {
                    _clearTimers.Dispose();
                    _clearTimers = null;
                }
            }

            stack.Push(obj);
        }

        /// <summary>借出</summary>
        /// <returns></returns>
        public virtual T Pop()
        {
            var stack = Stock;

            T obj;
            if (stack.TryPop(out obj))
            {
                Debug.Assert(obj != null);
                return obj;
            }

            obj = Create();
            Interlocked.Increment(ref _CreateCount);

            return obj;
        }

        /// <summary>创建实例</summary>
        /// <returns></returns>
        protected virtual T Create()
        {
            if (OnCreate != null) return OnCreate();

            return (T)(typeof(T).CreateInstance());
        }
        #endregion

        #region 清理定时器
        TimerX _clearTimers;

        void ClearAboveMax(Object state)
        {
            var stack = Stock;

            // 把超标的全部清了
            while (stack.Count > Max)
            {
                //var obj = stack.Pop() as IDisposable;
                //if (obj != null) obj.Dispose();
                state.TryDispose();

                Interlocked.Increment(ref _FreeCount);
            }

            // 没到最大值，关闭定时器？
            if (stack.Count < Max - 100 && _clearTimers != null)
            {
                _clearTimers.Dispose();
                _clearTimers = null;
            }
        }
        #endregion

        #region 释放资源
        /// <summary>子类重载实现资源释放逻辑</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            if (disposing)
            {
                // 释放托管资源
                if (_Stock != null)
                {
                    //_Stock.Dispose();
                    _Stock.TryDispose();
                    _Stock = null;
                }
            }

            if (_clearTimers != null)
            {
                _clearTimers.Dispose();
                _clearTimers = null;
            }
        }
        #endregion
    }
}