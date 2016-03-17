using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife.Net;
using NewLife.Threading;

namespace NewLife.MessageQueue
{
    /// <summary>主题</summary>
    public class Topic
    {
        #region 属性
        public String Name { get; set; }

        //public List<MQSession> Publicers { get; private set; }

        public List<MQSession> Subscribers { get; private set; }

        public Queue<String> Queue { get; private set; }
        #endregion

        #region 构造函数
        public Topic()
        {
            //Publicers = new List<MQSession>();
            Subscribers = new List<MQSession>();
            Queue = new Queue<String>();
        }
        #endregion

        #region 进入队列
        public Boolean Enqueue(String msg)
        {
            if (Queue.Count > 10000) return false;

            Queue.Enqueue(msg);

            Notify();

            return true;
        }
        #endregion

        #region 推送消息
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

            ThreadPoolX.QueueUserWorkItem(() =>
            {
                while (Queue.Count > 0)
                {
                    // 消息出列
                    var item = Queue.Dequeue();
                    // 向每一个订阅者推送消息
                    foreach (var ss in Subscribers)
                    {
                        ss.SendMessage(item);
                    }
                }
            });
        }
        #endregion
    }
}