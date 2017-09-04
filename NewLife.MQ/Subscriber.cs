using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace NewLife.MessageQueue
{
    /// <summary>订阅者</summary>
    public class Subscriber
    {
        /// <summary>主机</summary>
        public Consumer Host { get; internal set; }

        /// <summary>用户</summary>
        public Object User { get; }

        /// <summary>标签</summary>
        public ICollection<String> Tags { get; }

        /// <summary>消费委托。需要考虑订阅者销毁了而没有取消注册</summary>
        public Func<Subscriber, Message, Task> OnMessage { get; }

        /// <summary>实例化</summary>
        /// <param name="user"></param>
        /// <param name="tag"></param>
        /// <param name="onMessage"></param>
        public Subscriber(Object user, String tag, Func<Subscriber, Message, Task> onMessage)
        {
            User = user;
            if (!tag.IsNullOrEmpty()) Tags = new HashSet<String>(tag.Split("||", ",", ";"));
            OnMessage = onMessage;
        }

        /// <summary>增加标签</summary>
        /// <param name="tag"></param>
        public void AddTag(String tag)
        {
            var tags = tag.Split("||", ",", ";");
            foreach (var item in tags)
            {
                if (!Tags.Contains(item)) Tags.Add(item);
            }
        }

        /// <summary>是否匹配该订阅者</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Boolean IsMatch(Message msg)
        {
            return Tags == null || msg.Tag == null || Tags.Contains(msg.Tag);
        }

        /// <summary>发送消息给订阅者</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task NoitfyAsync(Message msg)
        {
            await TaskEx.Run(() => OnMessage(this, msg));
        }
    }
}