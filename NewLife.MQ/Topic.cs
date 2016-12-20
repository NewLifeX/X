using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private List<Subscriber> Subscribers { get; } = new List<Subscriber>();

        /// <summary>消息队列</summary>
        public Queue<Message> Queue { get; } = new Queue<Message>();
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public Topic()
        {
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
                    foreach (var ss in Subscribers)
                    {
                        await ss.NoitfyAsync(msg);
                    }
                }
            });
        }
        #endregion
    }
}