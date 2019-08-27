#if NET4
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Runtime.CompilerServices;

/// <summary>await��չ</summary>
public static class AwaitExtensions
{
    /// <summary></summary>
    /// <param name="source"></param>
    /// <param name="dueTime"></param>
	public static void CancelAfter(this CancellationTokenSource source, int dueTime)
    {
        if (source == null)
        {
            throw new NullReferenceException();
        }
        if (dueTime < -1)
        {
            throw new ArgumentOutOfRangeException("dueTime");
        }
        Contract.EndContractBlock();
        Timer timer = null;
        timer = new Timer(delegate (object state)
        {
            timer.Dispose();
            TimerManager.Remove(timer);
            try
            {
                source.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }, null, -1, -1);
        TimerManager.Add(timer);
        timer.Change(dueTime, -1);
    }

    /// <summary></summary>
    /// <param name="source"></param>
    /// <param name="dueTime"></param>
	public static void CancelAfter(this CancellationTokenSource source, TimeSpan dueTime)
    {
        long num = (long)dueTime.TotalMilliseconds;
        if (num < -1L || num > 2147483647L)
        {
            throw new ArgumentOutOfRangeException("dueTime");
        }
        source.CancelAfter((int)num);
    }

    /// <summary>��ȡ�ȴ���</summary>
    /// <param name="task"></param>
    /// <returns></returns>
	public static TaskAwaiter GetAwaiter(this Task task)
    {
        if (task == null)
        {
            throw new ArgumentNullException("task");
        }
        return new TaskAwaiter(task);
    }

    /// <summary>��ȡ�ȴ���</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="task"></param>
    /// <returns></returns>
	public static TaskAwaiter<TResult> GetAwaiter<TResult>(this Task<TResult> task)
    {
        if (task == null)
        {
            throw new ArgumentNullException("task");
        }
        return new TaskAwaiter<TResult>(task);
    }

    /// <summary>����await�Ƿ񲶻�������</summary>
    /// <param name="task"></param>
    /// <param name="continueOnCapturedContext"></param>
    /// <returns></returns>
    public static ConfiguredTaskAwaitable ConfigureAwait(this Task task, bool continueOnCapturedContext)
    {
        if (task == null) throw new ArgumentNullException("task");

        return new ConfiguredTaskAwaitable(task, continueOnCapturedContext);
    }

    /// <summary>����await�Ƿ񲶻�������</summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="task"></param>
    /// <param name="continueOnCapturedContext"></param>
    /// <returns></returns>
    public static ConfiguredTaskAwaitable<TResult> ConfigureAwait<TResult>(this Task<TResult> task, bool continueOnCapturedContext)
    {
        if (task == null) throw new ArgumentNullException("task");

        return new ConfiguredTaskAwaitable<TResult>(task, continueOnCapturedContext);
    }
}
#endif