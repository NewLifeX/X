using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Threading;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.MessageQueue
{
    /// <summary>主题</summary>
    /// <remarks>
    /// 每一个主题可选广播消费或集群消费，默认集群消费。
    /// </remarks>
    public class Topic
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; }

        /// <summary>主机</summary>
        public MQHost Host { get; internal set; }

        /// <summary>广播消费模式，消息推送给集群（相同User）内所有客户端</summary>
        public Boolean Broadcast { get; set; }

        /// <summary>消费者集群</summary>
        private ConcurrentDictionary<String, Consumer> Consumers { get; } = new ConcurrentDictionary<String, Consumer>();

        /// <summary>消息队列</summary>
        public ConcurrentQueue<Message> Queue { get; } = new ConcurrentQueue<Message>();
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public Topic(MQHost host, String name)
        {
            Host = host;
            Name = name;
        }
        #endregion

        #region 订阅管理
        /// <summary>订阅主题</summary>
        /// <param name="user">消费者</param>
        /// <param name="tag">标签。消费者用于在主题队列内部过滤消息</param>
        /// <param name="onMessage">消费消息的回调函数</param>
        /// <param name="userState">订阅者</param>
        /// <returns></returns>
        public Boolean Add(String user, String tag, Func<Subscriber, Message, Task> onMessage, Object userState)
        {
            //if (Subscribers.ContainsKey(user)) return false;
            //Consumer cs = null;
            //if (!Consumers.TryGetValue(user, out cs))
            //{
            //    // 新增消费者集群
            //    cs = new Consumer(user);
            //    Consumers[user] = cs;
            //}
            // 新增消费者集群
            var cs = Consumers.GetOrAdd(user, e => new Consumer(e) { Host = this });

            cs.Add(userState, tag, onMessage);

            // 可能是第一个订阅者，赶紧消费积累下来的消息
            Notify();

            return true;
        }

        /// <summary>取消订阅</summary>
        /// <param name="user">订阅者</param>
        /// <param name="userState">订阅者</param>
        /// <returns></returns>
        public Boolean Remove(String user, Object userState)
        {
            //if (!Consumers.Remove(user)) return false;
            if (!Consumers.TryGetValue(user, out var cs)) return false;

            var rs = cs.Remove(userState);

            // 如果没有订阅者，则删除消费者
            if (rs && cs.Subscribers.Count == 0) Consumers.Remove(user);

            return rs;
        }
        #endregion

        #region 进入队列
        /// <summary>进入队列</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Int32 Send(Message msg)
        {
            if (Queue.Count > 10000) return -1;

            Queue.Enqueue(msg);

            Notify();

            return Consumers.Count;
        }
        #endregion

        #region 推送消息
        /// <summary>推送通知</summary>
        private void Notify()
        {
            // 扫描一次，一旦发送有消息，则调用线程池线程处理
            if (_Timer == null)
                _Timer = new TimerX(Push, null, 0, 5000, Host?.Name) { Async = true };
            else
                _Timer.SetNext(-1);
        }

        private TimerX _Timer;

        private async void Push(Object state)
        {
            if (Queue.Count == 0) return;

            var ss = Consumers.ToValueArray();
            if (ss.Count == 0) return;

            // 消息出列
            Message msg = null;
            while (Queue.TryDequeue(out msg))
            {
                // 向每一个订阅者推送消息
                try
                {
                    await Dispatch(msg, ss);
                }
                catch { }
            }
        }

        private async Task<Int32> Dispatch(Message msg, IEnumerable<Consumer> ss)
        {
            var ts = new List<Task>();
            // 向每一个订阅者推送消息
            foreach (var item in ss)
            {
                ts.Add(item.Dispatch(msg));
            }
            // 一起等待
            await TaskEx.WhenAll(ts);

            return ts.Count;
        }
        #endregion

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