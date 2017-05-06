using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.MessageQueue
{
    /// <summary>消息队列主机</summary>
    public class MQHost : DisposeBase
    {
        #region 静态单一实例
        /// <summary>默认实例</summary>
        public static MQHost Instance { get; } = new MQHost();
        #endregion

        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>上线下线提示</summary>
        public Boolean Tip { get; set; }

        ///// <summary>统计</summary>
        //public IStatistics Stat { get; } = new Statistics();
        #endregion

        #region 构造函数
        /// <summary>实例化一个消息队列主机</summary>
        public MQHost()
        {
            Name = GetType().Name.TrimEnd("Host");
        }
        #endregion

        #region 主题
        /// <summary>主题集合</summary>
        private ConcurrentDictionary<String, Topic> Topics { get; } = new ConcurrentDictionary<String, Topic>(StringComparer.OrdinalIgnoreCase);

        /// <summary>获取或添加主题</summary>
        /// <param name="topic">主题</param>
        /// <param name="create">是否创建</param>
        /// <returns></returns>
        private Topic Get(String topic, Boolean create)
        {
            if (create) return Topics.GetOrAdd(topic, s => new Topic { Name = topic, Host = this });

            Topic tp = null;
            Topics.TryGetValue(topic, out tp);

            return tp;
        }
        #endregion

        #region 订阅管理
        /// <summary>订阅主题</summary>
        /// <param name="user">订阅者</param>
        /// <param name="topic">主题。沟通生产者消费者之间的桥梁</param>
        /// <param name="tag">标签。消费者用于在主题队列内部过滤消息</param>
        /// <param name="onMessage">消费消息的回调函数</param>
        /// <returns></returns>
        [DisplayName("订阅主题")]
        public void Subscribe(String user, String topic, String tag, Func<Message, Task> onMessage)
        {
            if (user.IsNullOrEmpty()) throw new ArgumentNullException(nameof(user));
            if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));
            if (onMessage == null) throw new ArgumentNullException(nameof(onMessage));

            WriteLog("{0}订阅（{1}, {2}）", user, topic, tag);

            var tp = Get(topic, true);
            if (tp != null)
            {
                var rs = tp.Add(user, tag, onMessage);
                // 提示其它订阅者
                if (rs && Tip) Send(user, topic, "Subscribe", "订阅主题");
            }
        }

        /// <summary>取消订阅</summary>
        /// <param name="user">订阅者</param>
        /// <param name="topic">主题。沟通生产者消费者之间的桥梁</param>
        [DisplayName("取消订阅")]
        public void Unsubscribe(String user, String topic = null)
        {
            if (user.IsNullOrEmpty()) throw new ArgumentNullException(nameof(user));

            if (!topic.IsNullOrEmpty())
            {
                WriteLog("取消订阅（{0}, {1}）", user, topic);

                var tp = Get(topic, false);
                if (tp != null)
                {
                    var rs = tp.Remove(user);
                    // 提示其它订阅者
                    if (rs && Tip) Send(user, topic, "Unsubscribe", "取消订阅");
                }
            }
            // 取消当前用户的所有订阅
            else
            {
                WriteLog("{0}取消所有{1}个订阅", user, Topics.Count);

                foreach (var item in Topics.Values)
                {
                    var rs = item.Remove(user);
                    // 提示其它订阅者
                    if (rs && Tip) Send(user, topic, "Unsubscribe", "取消订阅");
                }
            }
        }
        #endregion

        #region 发布管理
        /// <summary>单向发送。不需要反馈</summary>
        /// <param name="msg">消息</param>
        public Int32 Send(Message msg)
        {
            var tp = Get(msg.Topic, true);
            if (tp == null) throw new ArgumentNullException(nameof(msg.Topic), "找不到主题");

            return tp.Send(msg);
        }

        /// <summary>单向发送。不需要反馈</summary>
        /// <param name="user">生产者</param>
        /// <param name="topic">主题</param>
        /// <param name="tag">标签</param>
        /// <param name="content">内容</param>
        public Int32 Send(String user, String topic, String tag, Object content)
        {
            var msg = new Message
            {
                Topic = topic,
                Sender = user,
                Tag = tag,
                Content = content
            };

            return Send(msg);
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