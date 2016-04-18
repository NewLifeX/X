using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Threading
{
    /// <summary>任务助手</summary>
    public static class TaskHelper
    {
        /// <summary>捕获异常并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public static Task LogException(this Task task, ILog log = null)
        {
            //var flag = enable != null ? enable.Value : XTrace.Debug;
            //if (!flag) return task;
            if (log == null) log = XTrace.Log;
            if (log == Logger.Null || !log.Enable) return task;

            return task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null && t.Exception.InnerException != null) log.Error(null, t.Exception.InnerException);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>捕获异常并输出日志</summary>
        /// <param name="task"></param>
        /// <param name="errCallback"></param>
        /// <returns></returns>
        public static Task LogException(this Task task, Action<Exception> errCallback)
        {
            //var flag = enable != null ? enable.Value : XTrace.Debug;
            //if (!flag) return task;
            if (errCallback == null) return task;

            return task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null && t.Exception.InnerException != null) errCallback(t.Exception.InnerException);
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>异步读取数据流</summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Task<Int32> ReadAsync(this Stream stream, Byte[] buffer, Int32 offset, Int32 count)
        {
            return Task<Int32>.Factory.FromAsync<Byte[], Int32, Int32>(stream.BeginRead, stream.EndRead, buffer, offset, count, null);
        }

        /// <summary>异步读取数据</summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Task<Byte[]> ReadAsync(this Stream stream, Int32 length)
        {
            if (length <= 0) throw new ArgumentOutOfRangeException("length");

            var buffer = new Byte[length];
            var task = Task.Factory.FromAsync<Byte[], Int32, Int32, Int32>(stream.BeginRead, stream.EndRead, buffer, 0, length, null);
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
            return Task.Factory.FromAsync<Byte[], Int32, Int32>(stream.BeginWrite, stream.EndWrite, buffer, offset, count, null);
        }
    }
}