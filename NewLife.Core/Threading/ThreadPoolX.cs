using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Collections;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>轻量级线程池。无等待和调度逻辑，直接创建线程竞争处理器资源</summary>
    public class ThreadPoolX : DisposeBase
    {
        #region 全局线程池助手
        /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出</summary>
        /// <param name="callback"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem(Action callback)
        {
            if (callback == null) return;

            //ThreadPool.UnsafeQueueUserWorkItem(s =>
            //{
            //    try
            //    {
            //        callback();
            //    }
            //    catch (Exception ex)
            //    {
            //        XTrace.WriteException(ex);
            //    }
            //}, null);

            Instance.QueueWorkItem(callback);
        }

        /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出</summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem<T>(Action<T> callback, T state)
        {
            if (callback == null) return;

            Instance.QueueWorkItem(() => callback(state));
        }
        #endregion

        #region 静态实例
        /// <summary>静态实例</summary>
        public static ThreadPoolX Instance { get; } = new ThreadPoolX();
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
        #endregion
    }

    /// <summary>线程任务项</summary>
    public class ThreadItem : DisposeBase
    {
        #region 属性
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
        #endregion

        #region 方法
        /// <summary>执行委托</summary>
        /// <param name="callback"></param>
        public void Execute(Action callback)
        {
            var th = Thread;
            if (th == null)
            {
                th = Thread = new Thread(Work)
                {
                    Name = "P",
                    IsBackground = true,
                    //Priority = ThreadPriority.AboveNormal,
                };
                waitForTimer = new AutoResetEvent(false);

                Active = true;
                th.Start();
            }

            _callback = callback;

            waitForTimer.Set();
        }

        private Action _callback;
        private AutoResetEvent waitForTimer;
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
                Host.Pool.Put(this);

                waitForTimer.Reset();
                waitForTimer.WaitOne();
            }
        }
        #endregion
    }
}