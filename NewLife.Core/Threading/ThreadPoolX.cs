using System.Diagnostics;
using NewLife.Log;

namespace NewLife.Threading;

/// <summary>线程池助手</summary>
public class ThreadPoolX : DisposeBase
{
    #region 全局线程池助手
    static ThreadPoolX()
    {
        // 在这个同步异步大量混合使用的时代，需要更多的初始线程来屏蔽各种对TPL的不合理使用
        ThreadPool.GetMinThreads(out var wt, out var io);
        if (wt < 32 || io < 32)
        {
            if (wt < 32) wt = 32;
            if (io < 32) io = 32;
            ThreadPool.SetMinThreads(wt, io);
        }
    }

    /// <summary>初始化线程池
    /// </summary>
    public static void Init() { }

    /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出，同时不会捕获上下文</summary>
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

    /// <summary>带异常处理的线程池任务调度，不允许异常抛出，以免造成应用程序退出，同时不会捕获上下文</summary>
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
}