using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Threading;
#if NET4
using Task = System.Threading.Tasks.TaskEx;
#endif

namespace NewLife.Net
{
    /// <summary>数据包队列接口。用于把响应数据包配对到请求包</summary>
    public interface IPacketQueue
    {
        /// <summary>加入请求队列</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="request">请求的数据</param>
        /// <param name="remote">远程</param>
        /// <param name="msTimeout">超时取消时间</param>
        Task<Object> Add(Object owner, Object request, Int32 msTimeout);

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="response">响应的数据</param>
        /// <param name="remote">远程</param>
        /// <returns></returns>
        Boolean Match(Object owner, Object response, Func<Object, Object, Boolean> callback);
    }

    /// <summary>接收队列。子类可重载以自定义请求响应匹配逻辑</summary>
    public class DefaultPacketQueue : IPacketQueue
    {
        private LinkedList<Item> Items = new LinkedList<Item>();
        private TimerX _Timer;

        /// <summary>加入请求队列</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="request">请求的数据</param>
        /// <param name="remote">远程</param>
        /// <param name="msTimeout">超时取消时间</param>
        public virtual Task<Object> Add(Object owner, Object request, Int32 msTimeout)
        {
            var now = DateTime.Now;

            var qi = new Item
            {
                Owner = owner,
                Request = request,
                EndTime = now.AddMilliseconds(msTimeout),
                Source = new TaskCompletionSource<Object>()
            };

            // 加锁处理，更安全
            var qs = Items;
            lock (qs)
            {
                qs.AddLast(qi);
            }

            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = new TimerX(Check, null, 1000, 1000, "Packet");
                }
            }

            return qi.Source.Task;
        }

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="response">响应的数据</param>
        /// <param name="remote">远程</param>
        /// <returns></returns>
        public virtual Boolean Match(Object owner, Object response, Func<Object, Object, Boolean> callback)
        {
            var qs = Items;
            if (qs.Count == 0) return false;

            // 加锁复制以后再遍历，避免线程冲突
            var arr = qs.ToArray();
            foreach (var qi in arr)
            {
                if (qi.Owner == owner && callback(qi.Request, response))
                {
                    lock (qs)
                    {
                        qs.Remove(qi);
                    }

                    // 异步设置完成结果，否则可能会在当前线程恢复上层await，导致堵塞当前任务
                    if (!qi.Source.Task.IsCompleted) Task.Run(() => qi.Source.SetResult(response));

                    return true;
                }
            }

            //if (Setting.Current.Debug)
            //    XTrace.WriteLine("PacketQueue.CheckMatch 失败 [{0}] remote={1} Items={2}", response.Count, remote, arr.Length);

            return false;
        }

        private Int32 _Checking = 0;
        /// <summary>定时检查发送队列，超时未收到响应则重发</summary>
        /// <param name="state"></param>
        void Check(Object state)
        {
            var qs = Items;
            if (qs.Count == 0)
            {
                _Timer.TryDispose();
                _Timer = null;
                return;
            }

            if (Interlocked.CompareExchange(ref _Checking, 1, 0) != 0) return;

            try
            {
                if (qs.Count == 0) return;

                var now = DateTime.Now;
                // 加锁复制以后再遍历，避免线程冲突
                foreach (var qi in qs.ToArray())
                {
                    // 过期取消
                    if (qi.EndTime <= now)
                    {
                        qs.Remove(qi);

                        if (!qi.Source.Task.IsCompleted) qi.Source.SetCanceled();
                    }
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref _Checking, 0, 1);
            }
        }

        class Item
        {
            public Object Owner { get; set; }
            public Object Request { get; set; }
            public DateTime EndTime { get; set; }
            public TaskCompletionSource<Object> Source { get; set; }
        }
    }
}