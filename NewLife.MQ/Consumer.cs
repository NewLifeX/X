using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.MessageQueue
{
    /// <summary>消费者。相同标识的多个订阅者，构成消费者集群</summary>
    public class Consumer
    {
        /// <summary>主机</summary>
        public Topic Host { get; internal set; }

        /// <summary>用户</summary>
        public String User { get; }

        /// <summary>订阅者</summary>
        public ConcurrentDictionary<Object, Subscriber> Subscribers { get; } = new ConcurrentDictionary<Object, Subscriber>();

        /// <summary>实例化</summary>
        /// <param name="user"></param>
        public Consumer(String user)
        {
            User = user;
        }

        #region 订阅管理
        /// <summary>添加订阅者</summary>
        /// <param name="user">订阅者</param>
        /// <param name="tag">标签。消费者用于在主题队列内部过滤消息</param>
        /// <param name="onMessage">消费消息的回调函数</param>
        /// <returns></returns>
        public Boolean Add(Object user, String tag, Func<Subscriber, Message, Task> onMessage)
        {
            if (user == null) user = "";
            if (Subscribers.ContainsKey(user)) return false;

            var scb = new Subscriber(user, tag, onMessage);
            scb.Host = this;
            Subscribers[user] = scb;

            // 自动删除
            var dp = user as IDisposable2;
            if (dp != null) dp.OnDisposed += (s, e) => Remove(user);

            return true;
        }

        /// <summary>移除订阅者</summary>
        /// <param name="user">订阅者</param>
        /// <returns></returns>
        public Boolean Remove(Object user)
        {
            if (user == null) user = "";
            if (!Subscribers.Remove(user)) return false;

            return true;
        }
        #endregion

        private Int32 _next = 0;
        internal async Task<Boolean> Dispatch(Message msg)
        {
            // 向其中一个订阅者推送消息
            var ss = Subscribers.ToValueArray();
            for (int i = 0; i < ss.Length; i++)
            {
                var idx = _next + i;
                if (idx >= ss.Length) idx = 0;

                var item = ss[idx];
                if (item.IsMatch(msg))
                {
                    try
                    {
                        await item.NoitfyAsync(msg);
                        _next = idx + 1;
                        return true;
                    }
                    catch { }
                }
            }

            return false;
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}