using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var host = Session.GetService<ApiServer>();
            var topics = host["Topics"] as IDictionary<String, Topic>;

            Topic tp = null;
            if (topics.TryGetValue(topic, out tp)) return tp;

            if (!create) throw new Exception("主题[{0}]不存在".F(topic));

            tp = new Topic();
            tp.Name = topic;

            topics[topic] = tp;

            return tp;
        }

        /// <summary>创建主题</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        [DisplayName("创建主题")]
        public Boolean Create(String topic)
        {
            XTrace.WriteLine("创建主题 {0} @{1}", topic, Session["user"]);

            Check(topic, true);

            return true;
        }

        /// <summary>订阅</summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        [DisplayName("订阅主题")]
        public Boolean Subscribe(String topic)
        {
            XTrace.WriteLine("订阅主题 {0} @{1}", topic, Session["user"]);

            var tp = Check(topic, false);

            return true;
        }
    }
}