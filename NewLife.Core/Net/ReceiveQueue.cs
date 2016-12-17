using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Collections;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>接收队列。子类可重载以自定义请求响应匹配逻辑</summary>
    public class ReceiveQueue
    {
        private ConcurrentHashSet<Item> Items { get; set; } = new ConcurrentHashSet<Item>();
        private TimerX _Timer;

        /// <summary>加入请求队列</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="remote">远程</param>
        /// <param name="request">请求的数据</param>
        /// <param name="msTimeout">超时取消时间</param>
        public virtual Task<Byte[]> Add(Object owner, IPEndPoint remote, Byte[] request, Int32 msTimeout)
        {
            var now = DateTime.Now;

            var qi = new Item();
            qi.Owner = owner;
            qi.Request = request;
            qi.Remote = remote;
            qi.EndTime = now.AddMilliseconds(msTimeout);
            qi.Source = new TaskCompletionSource<Byte[]>();

            if (!Items.TryAdd(qi)) throw new Exception("加入请求队列失败！");

            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = new TimerX(Check, null, 0, 1000);
                }
            }

            return qi.Source.Task;
        }

        /// <summary>检查请求队列是否有匹配该响应的请求</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="remote">远程</param>
        /// <param name="response">响应的数据</param>
        /// <returns></returns>
        public virtual Boolean Match(Object owner, IPEndPoint remote, Byte[] response)
        {
            if (Items.IsEmpty) return false;

            foreach (var qi in Items)
            {
                if (qi.Owner == owner && (qi.Remote == null || remote == null || qi.Remote == remote) && IsMatch(owner, remote, qi.Request, response))
                {
                    Items.TryRemove(qi);

                    if (!qi.Source.Task.IsCompleted) qi.Source.SetResult(response);

                    return true;
                }
            }

            return false;
        }

        /// <summary>请求和响应是否匹配</summary>
        /// <param name="owner">拥有者</param>
        /// <param name="remote">远程</param>
        /// <param name="request">请求的数据</param>
        /// <param name="response">响应的数据</param>
        /// <returns></returns>
        protected virtual Boolean IsMatch(Object owner, IPEndPoint remote, Byte[] request, Byte[] response)
        {
            return true;
        }

        private Int32 _Checking = 0;
        /// <summary>定时检查发送队列，超时未收到响应则重发</summary>
        /// <param name="state"></param>
        void Check(Object state)
        {
            if (Items.IsEmpty) return;

            if (Interlocked.CompareExchange(ref _Checking, 1, 0) != 0) return;

            try
            {
                if (Items.IsEmpty) return;

                var now = DateTime.Now;
                foreach (var qi in Items)
                {
                    if (qi.EndTime <= now)
                    {
                        Items.TryRemove(qi);

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
            public Byte[] Request { get; set; }
            public IPEndPoint Remote { get; set; }
            public DateTime EndTime { get; set; }
            public TaskCompletionSource<Byte[]> Source { get; set; }
        }
    }
}