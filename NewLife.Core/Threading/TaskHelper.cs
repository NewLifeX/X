using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NewLife.Log;

namespace System.Threading.Tasks
{
    /// <summary>任务助手</summary>
    public static class TaskHelper
    {
        #region 任务已完成
        /// <summary>是否正确完成</summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static Boolean IsOK(this Task task) => task != null && task.Status == TaskStatus.RanToCompletion;
        #endregion

        #region 异常日志/执行时间
        /// <summary>捕获异常并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Task LogException(this Task task, ILog log = null)
        {
            if (task == null) return null;

            if (log == null) log = XTrace.Log;
            if (log == Logger.Null || !log.Enable) return task;

            return task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null && t.Exception.InnerException != null) log.Error(null, t.Exception.InnerException);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>捕获异常并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Task<TResult> LogException<TResult>(this Task<TResult> task, ILog log = null)
        {
            if (task == null) return null;

            if (log == null) log = XTrace.Log;
            if (log == Logger.Null || !log.Enable) return task;

            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null && t.Exception.InnerException != null) log.Error(null, t.Exception.InnerException);
            }, TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        /// <summary>捕获异常并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="errCallback"></param>
        /// <returns></returns>
        public static Task LogException(this Task task, Action<Exception> errCallback)
        {
            if (task == null) return null;

            if (errCallback == null) return task;

            return task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null && t.Exception.InnerException != null) errCallback(t.Exception.InnerException);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>统计时间并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Task LogTime(this Task task, String name, ILog log = null)
        {
            if (task == null) return null;

            if (log == null) log = XTrace.Log;
            if (log == Logger.Null || !log.Enable) return task;

            var sw = Stopwatch.StartNew();

            return task.ContinueWith(t =>
            {
                sw.Stop();
                log.Info("{0} 耗时 {0}ms", name, sw.ElapsedMilliseconds);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        /// <summary>统计时间并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="name"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Task<TResult> LogTime<TResult>(this Task<TResult> task, String name, ILog log = null)
        {
            if (task == null) return null;

            if (log == null) log = XTrace.Log;
            if (log == Logger.Null || !log.Enable) return task;

            var sw = Stopwatch.StartNew();

            return task.ContinueWith(t =>
            {
                sw.Stop();
                log.Info("{0} 耗时 {1}ms", name, sw.ElapsedMilliseconds);
                return t.Result;
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
        #endregion

        #region 数据流异步
#if !__CORE__
        /// <summary>异步读取数据流</summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Task<Int32> ReadAsync(this Stream stream, Byte[] buffer, Int32 offset, Int32 count)
        {
            return Task<Int32>.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, offset, count, null);
        }

        /// <summary>异步读取数据</summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Task<Byte[]> ReadAsync(this Stream stream, Int32 length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException("length");

            var buffer = new Byte[length];
            var task = Task.Factory.FromAsync(stream.BeginRead, stream.EndRead, buffer, 0, length, null);
            return task.ContinueWith(t =>
            {
                var len = t.Result;
                if (len == length) return buffer;

                return buffer.ReadBytes(len);
            });
        }

        /// <summary>异步写入数据流</summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Task WriteAsync(this Stream stream, Byte[] buffer, Int32 offset, Int32 count)
        {
            return Task.Factory.FromAsync(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null);
        }
#endif
        #endregion

        #region 任务转换
        /// <summary>任务转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="task"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static Task<TResult> ToTask<TResult>(this Task task, CancellationToken cancellationToken = default(CancellationToken), TResult result = default(TResult))
        {
            if (task == null) return null;

            if (task.IsCompleted)
            {
                if (task.IsFaulted) return FromErrors<TResult>(task.Exception.InnerExceptions);

                if (task.IsCanceled || cancellationToken.IsCancellationRequested) return Canceled<TResult>();

                if (task.Status == TaskStatus.RanToCompletion) return FromResult<TResult>(result);
            }
            return ToTaskContinuation(task, result);
        }

        private static Task<TResult> FromErrors<TResult>(IEnumerable<Exception> exceptions)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetException(exceptions);
            return tcs.Task;
        }

        private static Task<TResult> Canceled<TResult>()
        {
            return CancelCache<TResult>.Canceled;
        }

        private static class CancelCache<TResult>
        {
            public static readonly Task<TResult> Canceled = GetCancelledTask();

            private static Task<TResult> GetCancelledTask()
            {
                var tcs = new TaskCompletionSource<TResult>();
                tcs.SetCanceled();
                return tcs.Task;
            }
        }

        private static Task<TResult> FromResult<TResult>(TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        private static Task<TResult> ToTaskContinuation<TResult>(Task task, TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>();
            task.ContinueWith(delegate (Task innerTask)
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    tcs.TrySetResult(result);
                    return;
                }
                tcs.TrySetFromTask(innerTask);
            }, TaskContinuationOptions.ExecuteSynchronously);
            return tcs.Task;
        }

        private static Boolean TrySetFromTask<TResult>(this TaskCompletionSource<TResult> tcs, Task source)
        {
            if (source.Status == TaskStatus.Canceled) return tcs.TrySetCanceled();

            if (source.Status == TaskStatus.Faulted) return tcs.TrySetException(source.Exception.InnerExceptions);

            if (source.Status == TaskStatus.RanToCompletion)
            {
                var task = source as Task<TResult>;
                return tcs.TrySetResult((task == null) ? default(TResult) : task.Result);
            }
            return false;
        }
        #endregion
    }
}