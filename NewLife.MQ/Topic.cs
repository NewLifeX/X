using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Remoting;
using NewLife.Threading;

namespace NewLife.MessageQueue
{
    /// <summary>主题</summary>
    public class Topic
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>订阅者</summary>
        private Dictionary<String, Subscriber> Subscribers { get; } = new Dictionary<String, Subscriber>();

        /// <summary>消息队列</summary>
        public Queue<Message> Queue { get; } = new Queue<Message>();
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public Topic()
        {
        }
        #endregion

        #region 订阅管理
        /// <summary>订阅主题</summary>
        /// <param name="user"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public Boolean Add(String user, IApiSession session)
        {
            if (Subscribers.ContainsKey(user)) return false;

            var scb = new Subscriber
            {
                User = user,
                Session = session
            };
            Subscribers[user] = scb;

            var ds = session as IDisposable2;
            if (ds != null) ds.OnDisposed += (s, e) => Remove(user);

#if DEBUG
            var msg = new Message
            {
                Sender = user,
                Body = "上线啦".GetBytes()
            };
            Enqueue(msg);
#endif

            return true;
        }

        /// <summary>取消订阅</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public Boolean Remove(String user)
        {
            if (!Subscribers.Remove(user)) return false;

#if DEBUG
            var msg = new Message
            {
                Sender = user,
                Body = "下线啦".GetBytes()
            };
            Enqueue(msg);
#endif

            return true;
        }
        #endregion

        #region 进入队列
        /// <summary>进入队列</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Boolean Enqueue(Message msg)
        {
            if (Queue.Count > 10000) return false;

            Queue.Enqueue(msg);

            Notify();

            return true;
        }
        #endregion

        #region 推送消息
        /// <summary>推送通知</summary>
        private void Notify()
        {
            // 扫描一次，一旦发送有消息，则调用线程池线程处理
            if (_Timer == null)
                _Timer = new TimerX(Push, null, 0, 5000);
            else
                _Timer.NextTime = DateTime.MinValue;
        }

        private TimerX _Timer;

        private void Push(Object state)
        {
            if (Queue.Count == 0) return;
            if (Subscribers.Count == 0) return;

            Task.Factory.StartNew(async () =>
            {
                while (Queue.Count > 0)
                {
                    // 消息出列
                    var msg = Queue.Dequeue();
                    // 向每一个订阅者推送消息
                    foreach (var ss in Subscribers.Values.ToArray())
                    {
                        if (ss.User != msg.Sender)
                            await ss.NoitfyAsync(msg);
                    }
                }
            });
        }
        #endregion
    }
}