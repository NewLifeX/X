using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>线程池扩展</summary>
    public static class ThreadPoolX
    {
        #region 全局线程池助手
        /// <summary>带异常处理的线程池任务调度</summary>
        /// <param name="callback"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem(WaitCallback callback) { QueueUserWorkItem(callback, null); }

        /// <summary>带异常处理的线程池任务调度</summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem(WaitCallback callback, Object state) { QueueUserWorkItem(callback, state, ex => XTrace.Log.Debug(null, ex)); }

        /// <summary>带异常处理的线程池任务调度，即使不指定异常处理方法，也不允许异常抛出，以免造成应用程序退出</summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="errCallback">发生异常时调用的方法</param>
        [DebuggerHidden]
        public static Task QueueUserWorkItem(WaitCallback callback, Object state, Action<Exception> errCallback)
        {
            if (callback == null) return null;

            //var cb = new WaitCallback(s =>
            //{
            //    var ss = (Object[])s;
            //    var wcb = ss[0] as WaitCallback;
            //    var st = ss[1];
            //    var onerr = ss[2] as Action<Exception>;

            //    try
            //    {
            //        wcb(st);
            //    }
            //    catch (Exception ex)
            //    {
            //        if (onerr != null)
            //        {
            //            try { onerr(ex); }
            //            catch { }
            //        }
            //    }
            //});

            //ThreadPool.QueueUserWorkItem(cb, new Object[] { callback, state, errCallback });

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    callback(state);
                }
                catch (Exception ex)
                {
                    if (errCallback != null)
                    {
                        try { errCallback(ex); }
                        catch { }
                    }
                }
            });
        }

        /// <summary>带异常处理的线程池任务调度</summary>
        /// <param name="callback"></param>
        [DebuggerHidden]
        public static void QueueUserWorkItem(Action callback)
        {
            QueueUserWorkItem(callback, ex =>
            {
                if (XTrace.Debug) XTrace.WriteException(ex);
            });
        }

        /// <summary>带异常处理的线程池任务调度，即使不指定异常处理方法，也不允许异常抛出，以免造成应用程序退出</summary>
        /// <param name="callback"></param>
        /// <param name="errCallback">发生异常时调用的方法</param>
        [DebuggerHidden]
        public static Task QueueUserWorkItem(Action callback, Action<Exception> errCallback)
        {
            if (callback == null) return null;

            //var cb = new WaitCallback(s =>
            //{
            //    var ss = (Object[])s;
            //    var func = ss[0] as Func;
            //    var onerr = ss[1] as Action<Exception>;

            //    try
            //    {
            //        func();
            //    }
            //    catch (Exception ex)
            //    {
            //        if (onerr != null)
            //        {
            //            try { onerr(ex); }
            //            catch { }
            //        }
            //    }
            //});

            //ThreadPool.QueueUserWorkItem(cb, new Object[] { callback, errCallback });

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    if (errCallback != null)
                    {
                        try { errCallback(ex); }
                        catch { }
                    }
                }
            });
        }
        #endregion
    }
}