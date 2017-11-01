using System;
using System.Diagnostics;
using System.Threading;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>线程池扩展</summary>
    public static class ThreadPoolX
    {
        #region 全局线程池助手
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
        }
        #endregion
    }
}