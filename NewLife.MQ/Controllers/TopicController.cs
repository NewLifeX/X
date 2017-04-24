using System;
using System.Collections.Generic;
using System.ComponentModel;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    /// <summary>主题控制器</summary>
    public class TopicController : IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        private Topic Check(String topic, Boolean create)
        {
            var host = (Session.Host as ApiServer).Provider as MQServer;

            Topic tp = null;
            if (host.Topics.TryGetValue(topic, out tp)) return tp;

            if (!create) throw new Exception("主题[{0}]不存在".F(topic));

            tp = new Topic();
            tp.Name = topic;

            host.Topics[topic] = tp;

            return tp;
        }

        ///// <summary>创建主题</summary>
        ///// <param name="topic"></param>
        ///// <returns></returns>
        //[DisplayName("创建主题")]
        //public Boolean Create(String topic)
        //{
        //    XTrace.WriteLine("创建主题 {0} @{1}", topic, Session["user"]);

        //    var tp = Check(topic, true);

        //    Session["Topic"] = tp;

        //    return true;
        //}

        /// <summary>订阅</summary>
        /// <param name="topic">主题。沟通生产者消费者之间的桥梁</param>
        /// <param name="tag">标签。消费者用于在主题队列内部过滤消息</param>
        /// <returns></returns>
        [DisplayName("订阅主题")]
        public Boolean Subscribe(String topic, String tag)
        {
            XTrace.WriteLine("订阅主题 {0} @{1}", topic, Session["user"]);

            var tp = Check(topic, true);

            var ss = Session.UserState as MQSession;
            var user = ss.User;

            // 退订旧的
            var old = Session["Topic"] as Topic;
            if (old != null) old.Remove(user);

            // 订阅新的
            Session["Topic"] = tp;
            tp.Add(user, Session);

            return true;
        }

        /// <summary>发布消息</summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        [DisplayName("发布消息")]
        public Boolean Public(Message msg)
        {
            XTrace.WriteLine("发布消息 {0}", msg);

            var user = Session["user"] as String;

            var tp = Session["Topic"] as Topic;
            if (tp == null) throw new Exception("未订阅");

            msg.Sender = user;
            tp.Enqueue(msg);

            return true;
        }
    }
}