using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;

namespace NewLife.MessageQueue
{
    /// <summary>消息队列主机</summary>
    public class MQHost : DisposeBase
    {
        #region 主体
        /// <summary>主题集合</summary>
        private ConcurrentDictionary<String, Topic> Topics { get; } = new ConcurrentDictionary<String, Topic>(StringComparer.OrdinalIgnoreCase);

        /// <summary>获取或添加主题</summary>
        /// <param name="topic">主题</param>
        /// <param name="create">是否创建</param>
        /// <returns></returns>
        private Topic Get(String topic, Boolean create)
        {
            if (create) return Topics.GetOrAdd(topic, s => new Topic { Name = topic });

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
        public void Subscribe(String user, String topic, String tag, Func<Message, Boolean> onMessage)
        {
            if (user.IsNullOrEmpty()) throw new ArgumentNullException(nameof(user));
            if (topic.IsNullOrEmpty()) throw new ArgumentNullException(nameof(topic));
            if (onMessage == null) throw new ArgumentNullException(nameof(onMessage));

            var tp = Get(topic, true);
            tp.Add(user, tag, onMessage);
        }

        /// <summary>取消订阅</summary>
        /// <param name="user">订阅者</param>
        /// <param name="topic">主题。沟通生产者消费者之间的桥梁</param>
        [DisplayName("取消订阅")]
        public void Unsubscribe(String user, String topic)
        {

        }
        #endregion

        #region 发布管理
        /// <summary>可靠异步发布</summary>
        /// <param name="msg">消息</param>
        /// <returns></returns>
        public async Task SendAsync(Message msg)
        {
            var tp = Get(msg.Topic, false);
            if (tp == null) throw new ArgumentNullException(nameof(msg.Topic), "找不到主题");

            tp.Enqueue(msg);
        }

        /// <summary>可靠异步发布</summary>
        /// <param name="user">生产者</param>
        /// <param name="topic">主题</param>
        /// <param name="tag">标签</param>
        /// <param name="content">内容</param>
        /// <returns></returns>
        public async Task SendAsync(String user, String topic, String tag, Object content)
        {
            var msg = new Message
            {
                Topic = topic,
                Sender = user,
                Tag = tag,
                Content = content
            };

            await SendAsync(msg);
        }

        /// <summary>单向发送。不需要反馈</summary>
        /// <param name="msg">消息</param>
        public void SendOneway(Message msg)
        {
            var tp = Get(msg.Topic, false);
            if (tp == null) throw new ArgumentNullException(nameof(msg.Topic), "找不到主题");

            tp.Enqueue(msg);
        }

        /// <summary>单向发送。不需要反馈</summary>
        /// <param name="user">生产者</param>
        /// <param name="topic">主题</param>
        /// <param name="tag">标签</param>
        /// <param name="content">内容</param>
        public void SendOneway(String user, String topic, String tag, Object content)
        {
            var msg = new Message
            {
                Topic = topic,
                Sender = user,
                Tag = tag,
                Content = content
            };

            SendOneway(msg);
        }
        #endregion
    }
}