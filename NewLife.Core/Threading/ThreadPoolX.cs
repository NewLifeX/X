using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>轻量级线程池。无等待和调度逻辑，直接创建线程竞争处理器资源</summary>
    public class ThreadPoolX : DisposeBase
    {
        #region 全局线程池助手
        static ThreadPoolX()
        {
            // 在这个同步异步大量混合使用的时代，需要更多的初始线程来屏蔽各种对TPL的不合理使用
            ThreadPool.GetMinThreads(out var wt, out var pt);
            if (wt < 32) ThreadPool.SetMinThreads(32, 32);
        }

        /// <summary>初始化线程池
        /// </summary>
        public static void Init() { }

        /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出</summary>
        /// <param name="callback"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem(Action callback)
        {
            if (callback == null) return;

            ThreadPool.UnsafeQueueUserWorkItem(s =>
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }, null);

            //Instance.QueueWorkItem(callback);
        }

        /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出</summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem<T>(Action<T> callback, T state)
        {
            if (callback == null) return;

            ThreadPool.UnsafeQueueUserWorkItem(s =>
            {
                try
                {
                    callback(state);
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }, null);

            //Instance.QueueWorkItem(() => callback(state));
        }
        #endregion

        #region 静态实例
        private static ThreadPoolX _Instance;
        /// <summary>静态实例</summary>
        public static ThreadPoolX Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (typeof(ThreadPoolX))
                    {
                        if (_Instance == null) _Instance = new ThreadPoolX();
                    }
                }

                return _Instance;
            }
        }
        #endregion

        #region 属性
        /// <summary>内部池</summary>
        public ObjectPool<ThreadItem> Pool { get; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ThreadPoolX()
        {
            var pool = new MyThreadPool
            {
                Name = nameof(ThreadPoolX),
                Host = this,

                Min = Environment.ProcessorCount,
                Max = 1000,

                IdleTime = 30,
                AllIdleTime = 120,
            };
            Pool = pool;
        }

        class MyThreadPool : ObjectPool<ThreadItem>
        {
            public ThreadPoolX Host { get; set; }

            /// <summary>创建实例</summary>
            /// <returns></returns>
            protected override ThreadItem OnCreate() => new ThreadItem(Host);
        }
        #endregion

        #region 方法
        /// <summary>把委托放入线程池执行</summary>
        /// <param name="callback"></param>
        public void QueueWorkItem(Action callback)
        {
            if (callback == null) return;

            var ti = Pool.Get();
            ti.Execute(callback);
        }

        /// <summary>在线程池中异步执行任务</summary>
        /// <param name="function"></param>
        /// <returns></returns>
        public Task QueueTask(Action function)
        {
            if (function == null) return null;

            return QueueTask<Object>(token => { function(); return null; }, CancellationToken.None);
        }

        /// <summary>在线程池中异步执行任务</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
        public Task<TResult> QueueTask<TResult>(Func<TResult> function)
        {
            if (function == null) return null;

            return QueueTask(token => function(), CancellationToken.None);
        }

        /// <summary>在线程池中异步执行任务</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="function"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<TResult> QueueTask<TResult>(Func<CancellationToken, TResult> function, CancellationToken cancellationToken)
        {
            if (function == null) return null;

            var source = new TaskCompletionSource<TResult>();

            var ti = Pool.Get();
            ti.Execute(() =>
            {
                try
                {
                    var rs = function(cancellationToken);
                    source.SetResult(rs);
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });

            return source.Task;
        }
        #endregion
    }

    /// <summary>线程任务项</summary>
    public class ThreadItem : DisposeBase
    {
        #region 属性
        /// <summary>编号</summary>
        public Int32 ID { get; private set; }

        /// <summary>线程</summary>
        public Thread Thread { get; private set; }

        /// <summary>主机线程池</summary>
        public ThreadPoolX Host { get; }

        /// <summary>活跃</summary>
        public Boolean Active { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ThreadItem(ThreadPoolX host)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            var th = Thread = new Thread(Work)
            {
                Name = "P",
                IsBackground = true,
                //Priority = ThreadPriority.AboveNormal,
            };
            waitForTimer = new AutoResetEvent(false);
            ID = th.ManagedThreadId;

            Active = true;
            th.Start();
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            try
            {
                Active = false;
                waitForTimer?.Set();

                var th = Thread;
                if (th != null && th.IsAlive) th.Abort();
            }
            catch { }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => "P" + ID;
        #endregion

        #region 方法
        /// <summary>执行委托</summary>
        /// <param name="callback"></param>
        public void Execute(Action callback)
        {
            _callback = callback;
            _state = 1;

            waitForTimer.Set();
        }

        private Action _callback;
        private AutoResetEvent waitForTimer;
        private Int32 _state;
        private void Work()
        {
            while (Active)
            {
                try
                {
                    _callback?.Invoke();
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }

                _callback = null;

                // 回到线程池里
                if (Interlocked.CompareExchange(ref _state, 0, 1) == 1 && !Host.Pool.Put(this)) break;

                // 不能重置，如果外面先Set，这里再WaitOne，同样得到信号
                //waitForTimer.Reset();
                waitForTimer.WaitOne();
            }

            // 销毁
            Dispose();
        }
        #endregion
    }
}