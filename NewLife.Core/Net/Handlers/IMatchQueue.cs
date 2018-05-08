using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Net.Handlers
{
    /// <summary>消息匹配队列接口。用于把响应数据包配对到请求包</summary>
    public interface IMatchQueue
    {
        /// <summary>加入请求队列</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="request">请求消息</param>
        /// <param name="msTimeout">超时取消时间</param>
        /// <param name="source">任务源</param>
        Task<Object> Add(Object owner, Object request, Int32 msTimeout, TaskCompletionSource<Object> source);

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="response">响应消息</param>
        /// <param name="result">任务结果</param>
        /// <param name="callback">用于检查匹配的回调</param>
        /// <returns></returns>
        Boolean Match(Object owner, Object response, Object result, Func<Object, Object, Boolean> callback);
    }

    /// <summary>消息匹配队列。子类可重载以自定义请求响应匹配逻辑</summary>
    public class DefaultMatchQueue : IMatchQueue
    {
        private ConcurrentQueue<Item> Items = new ConcurrentQueue<Item>();
        private TimerX _Timer;

        /// <summary>加入请求队列</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="request">请求的数据</param>
        /// <param name="msTimeout">超时取消时间</param>
        /// <param name="source">任务源</param>
        public virtual Task<Object> Add(Object owner, Object request, Int32 msTimeout, TaskCompletionSource<Object> source)
        {
            var now = TimerX.Now;

            if (source == null) source = new TaskCompletionSource<Object>();
            var qi = new Item
            {
                Owner = owner,
                Request = request,
                EndTime = now.AddMilliseconds(msTimeout),
                Source = source,
            };

            // 加锁处理，更安全
            Items.Enqueue(qi);

            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = new TimerX(Check, null, 1000, 1000, "Match") { Async = true };
                }
            }

            return source.Task;
        }

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="response">响应消息</param>
        /// <param name="result">任务结果</param>
        /// <param name="callback">用于检查匹配的回调</param>
        /// <returns></returns>
        public virtual Boolean Match(Object owner, Object response, Object result, Func<Object, Object, Boolean> callback)
        {
            var qs = Items;
            if (qs.IsEmpty) return false;

            // 直接遍历，队列不会很长
            foreach (var qi in qs)
            {
                var src = qi.Source;
                if (src != null && qi.Owner == owner && callback(qi.Request, response))
                {
                    // 标记为已处理
                    qi.Source = null;

                    // 异步设置完成结果，否则可能会在当前线程恢复上层await，导致堵塞当前任务
                    //if (!qi.Source.Task.IsCompleted) Task.Factory.StartNew(() => qi.Source.TrySetResult(result));
                    if (!src.Task.IsCompleted) src.TrySetResult(result);

                    return true;
                }
            }

            //if (Setting.Current.Debug)
            XTrace.WriteLine("MatchQueue.Check 失败 [{0}] result={1} Items={2}", response, result, qs.Count);

            return false;
        }

        /// <summary>定时检查发送队列，超时未收到响应则重发</summary>
        /// <param name="state"></param>
        void Check(Object state)
        {
            var qs = Items;
            if (qs.IsEmpty)
            {
                _Timer.TryDispose();
                _Timer = null;
                return;
            }

            var now = TimerX.Now;
            // 直接遍历，队列不会很长
            foreach (var qi in qs)
            {
                // 过期取消
                var src = qi.Source;
                if (src != null && qi.EndTime <= now)
                {
                    qi.Source = null;

                    if (!src.Task.IsCompleted)
                    {
#if DEBUG
                        var msg = qi.Request as Messaging.DefaultMessage;
                        Log.XTrace.WriteLine("超时丢失消息 Seq={0}", msg.Sequence);
#endif
                        src.TrySetCanceled();
                    }
                }
            }

            // 清理无效项
            while (qs.TryPeek(out var qm) && qm.Source == null && qs.TryDequeue(out qm)) ;
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