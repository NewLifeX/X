using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Threading;

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
        Task<Packet> Add(Object owner, Packet request, IPEndPoint remote, Int32 msTimeout);

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="response">响应的数据</param>
        /// <param name="remote">远程</param>
        /// <returns></returns>
        Boolean Match(Object owner, Packet response, IPEndPoint remote);
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
        public virtual Task<Packet> Add(Object owner, Packet request, IPEndPoint remote, Int32 msTimeout)
        {
            var now = DateTime.Now;

            var qi = new Item();
            qi.Owner = owner;
            qi.Request = request;
            qi.Remote = remote;
            qi.EndTime = now.AddMilliseconds(msTimeout);
            qi.Source = new TaskCompletionSource<Packet>();

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
        public virtual Boolean Match(Object owner, Packet response, IPEndPoint remote)
        {
            var qs = Items;
            if (qs.Count == 0) return false;

            // 加锁复制以后再遍历，避免线程冲突
            var arr = qs.ToArray();
            foreach (var qi in arr)
            {
                if (qi.Owner == owner &&
                    (qi.Remote == null || remote == null || qi.Remote + "" == remote + "") &&
                    IsMatch(owner, remote, qi.Request, response))
                {
                    lock (qs)
                    {
                        qs.Remove(qi);
                    }

                    if (!qi.Source.Task.IsCompleted) qi.Source.SetResult(response);

                    return true;
                }
            }

            if (Setting.Current.NetDebug)
                XTrace.WriteLine("PacketQueue.CheckMatch 失败 [{0}] remote={1} Items={2}", response.Count, remote, arr.Length);

            return false;
        }

        /// <summary>请求和响应是否匹配</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="remote">远程</param>
        /// <param name="request">请求的数据</param>
        /// <param name="response">响应的数据</param>
        /// <returns></returns>
        protected virtual Boolean IsMatch(Object owner, IPEndPoint remote, Packet request, Packet response)
        {
            return true;
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
            public Packet Request { get; set; }
            public IPEndPoint Remote { get; set; }
            public DateTime EndTime { get; set; }
            public TaskCompletionSource<Packet> Source { get; set; }
        }
    }
}